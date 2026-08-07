## Context

Hoy el agente IA se sirve vía Open WebUI + su Tool Server: `AgentEndpoints.cs` expone ~20 endpoints bajo `/api/agent/*` (protegidos por una API key estática y `RequireRateLimiting("agent")`), cada uno resolviendo la persona actual con `ResolvePersonAsync` a partir de la cabecera `X-Open-WebUI-User-Email` y delegando en un comando/query MediatR de `Core/Features/Agent/*Handlers.cs` (varios de ellos marcados `IAgentAuditable`, auditados automáticamente por `AgentAuditBehavior<TRequest,TResponse>`). Open WebUI aporta la UI de chat, el bucle de tool-calling contra el modelo (vía LiteLLM, que sigue intacto) y su propio almacenamiento de conversaciones, además de un segundo cliente OIDC contra Keycloak (`cartera-openwebui`) documentado en `infra/KEYCLOAK.md`.

El resto de la API ya resuelve la persona autenticada con `CurrentUser.ResolveAsync(HttpContext, IAppDbContext)` (JWT de Keycloak, claim `sub`, con fallback de vinculación por email) — el mismo patrón usado por dashboard, comments, epics, etc. Este change reemplaza el transporte (Open WebUI + header de suplantación) por ese mismo mecanismo de autenticación de primera clase, sin tocar la lógica de negocio existente.

## Goals / Non-Goals

**Goals:**
- Chat de IA integrado en la app (panel lateral Angular), con historial de conversaciones persistido en la BD de la propia aplicación.
- Bucle de tool-calling ejecutado en el backend, reutilizando literalmente los comandos/queries MediatR ya existentes en `Core/Features/Agent/*` (sin reescribir lógica de negocio ni permisos).
- La identidad del usuario para las tools la resuelve el servidor a partir del JWT de la petición — nunca es un parámetro que el modelo pueda rellenar.
- Auditoría de tool calls sin cambios (`AgentActionLog` / `AgentAuditBehavior` ya es agnóstico del transporte).
- Retirada de `open-webui` de `docker-compose.yml` y de `/api/agent/*`.

**Non-Goals:**
- Streaming de la respuesta (se evalúa en una iteración posterior).
- Selección de modelo por el usuario (el modelo es config de la app).
- RAG de documentos / subida de ficheros / generación de artefactos tipo code-interpreter. `store_chart`/`store_export` (`AgentBlobStore`) no los invoca ningún handler existente — eran ganchos pensados para el intérprete de código de Open WebUI; se eliminan sin reemplazo.
- Rediseñar los ~20 casos de uso de `Core/Features/Agent/*` — se reutilizan tal cual, solo cambia quién los invoca.

## Decisions

**1. Los tools del modelo son un catálogo (`ChatToolCatalog`) que envuelve los `Agent*Command/Query` existentes, no una reescritura.**
Cada entrada del catálogo (`Core/Features/Chat/Tools/`) define: nombre, descripción (misma redacción que hoy en `.WithSummary`/`.WithDescription` de `AgentEndpoints.cs`), JSON Schema de parámetros, y una función `(JsonElement args, int personId, ISender sender, CancellationToken) → Task<object>` que construye el `Agent*Command`/`Query` correspondiente y lo envía con `ISender.Send`. Alternativa considerada: reescribir la lógica de negocio como nuevos "tool handlers" — rechazada, duplicaría ~20 casos de uso ya probados y correctos sin necesidad; el único código que cambia es el transporte (HTTP Tool Server → invocación in-process), y `AgentAuditBehavior` sigue auditando exactamente igual porque sigue viendo los mismos tipos `IAgentAuditable`.

**2. Abstracción `IChatCompletionClient` en Core, implementación con el SDK `OpenAI` en Infrastructure.**
Core no puede depender del SDK `OpenAI` (Clean Architecture: Core solo depende de MediatR/FluentValidation). Se define en `Core/Interfaces` una abstracción mínima con DTOs propios (`ChatCompletionRequest`, `ChatCompletionResponse`, `ChatToolCall`) y se implementa en `Infrastructure/Services/LiteLlmChatCompletionClient.cs` traduciendo hacia/desde los tipos del SDK `OpenAI` (`ChatClient` apuntando a la `base_url` de LiteLLM). Mismo patrón que `IEmbeddingService` / `BedrockEmbeddingService`. Alternativa considerada: referenciar el SDK `OpenAI` directamente desde `Core/Features/Chat` — rechazada, rompe la regla de dependencias de Clean Architecture del proyecto.

**3. Bucle de tool-calling síncrono dentro de la misma petición HTTP, con tope de iteraciones.**
`POST /api/chat/conversations/{id}/messages` ejecuta el turno completo (posibles varias idas y vueltas modelo↔tools) antes de responder, con un máximo de 5 iteraciones de tool-calling; si se alcanza el tope, se devuelve el último mensaje del modelo con un aviso. Alternativa considerada: cola de trabajo asíncrona + polling desde el frontend — rechazada por sobreingeniería dado que no hay streaming en v1 y el número de tool calls por turno es pequeño y acotado por el propio dominio (tareas de alto nivel, no automatización masiva).

**4. Nuevas entidades `Conversation` y `ChatMessage` en el dominio existente (EF Core + Postgres), sin componente de almacenamiento externo.**
`Conversation`: Id, PersonId (FK Person), Title, CreatedAt, UpdatedAt. `ChatMessage`: Id, ConversationId (FK Conversation), Role (enum `User`/`Assistant`/`Tool`, almacenado como string por convención del proyecto), Content, ToolCallsJson (nullable — tool calls solicitados por el modelo en un mensaje `Assistant`), ToolName/ToolCallId (nullable — para mensajes `Tool` con el resultado de una ejecución), CreatedAt. Mismo patrón que `Comment`/`AgentActionLog` (entidad simple, factory `Create` estático, `private set`).

**5. Nuevo endpoint group `/api/chat/*` con autenticación JWT estándar (`RequireAuthorization()` + `CurrentUser.ResolveAsync`), sin API key ni rate limiter dedicado.**
Sustituye a `/api/agent/*`, que se elimina junto con `AgentApiKeyFilter`, el grupo OpenAPI `"agent"` en `Program.cs`, y los endpoints públicos de charts/exports (sin uso, ver Non-Goals). El `PersonId` que llega a cada `Agent*Command` se sigue resolviendo en el servidor (ahora desde el JWT en vez de la cabecera), nunca desde un parámetro que el modelo pueda controlar — se mantiene exactamente el mismo modelo de seguridad de hoy, solo que ahora corre dentro del pipeline de autorización real de la API en vez de confiar en una cabecera y una API key compartida.

**6. System prompt propio y configurable.**
Un prompt base (recurso/constante en `Core/Features/Chat`, en español) que establece el rol del asistente; la guía de cuándo usar cada tool sigue viviendo en la descripción de cada tool (se reutiliza el texto ya redactado en `AgentEndpoints.cs`), igual que hoy.

## Risks / Trade-offs

- [Riesgo] Enviar el catálogo completo (~20 tools) en cada turno aumenta tokens de entrada/coste → Mitigación: aceptable para el volumen de uso interno esperado; si se vuelve un problema, se puede filtrar el catálogo por rol (Gestor vs Desarrollador) en una iteración posterior.
- [Riesgo] Sin streaming, un turno con varias llamadas a tools puede tardar varios segundos y el usuario no ve progreso → Mitigación: estado de "escribiendo…" en el panel mientras se resuelve la petición; explícitamente aceptado como no-goal de esta versión.
- [Riesgo] Bucle de tool-calling sin control podría encadenar llamadas indefinidamente → Mitigación: tope duro de 5 iteraciones por turno.
- [Riesgo] Retirar `/api/agent/*` rompe cualquier integración externa que dependiera de él → Mitigación: confirmado que su única consumidora era Open WebUI, que se retira en el mismo change; no hay otros clientes conocidos.
- [Riesgo] Migración de esquema (nuevas tablas) en el mismo change que retira infraestructura → Mitigación: migración aditiva estándar (mismo patrón que `AddWorkItemEmbeddings`), sin tocar tablas existentes; reversible con un `down` estándar de EF Core.

## Migration Plan

1. Backend: paquete NuGet `OpenAI`, `IChatCompletionClient` (Core) + `LiteLlmChatCompletionClient` (Infrastructure), config `LiteLlm:BaseUrl`/`LiteLlm:ApiKey`/`Chat:Model`.
2. Backend: entidades `Conversation`/`ChatMessage` + migración EF Core.
3. Backend: `ChatToolCatalog` envolviendo los `Agent*Command/Query` existentes.
4. Backend: `SendChatMessageCommand`/Handler con el bucle de tool-calling (tope 5 iteraciones) + comandos/queries de gestión de conversaciones (crear, listar, listar mensajes, borrar).
5. Backend: `ChatEndpoints.cs` (`/api/chat/*`, `RequireAuthorization()`), tests unitarios del bucle y de los endpoints.
6. Backend: eliminar `AgentEndpoints.cs`, `AgentApiKeyFilter`, grupo OpenAPI `"agent"`, política de rate limiting `"agent"`, y el blob store de charts/exports.
7. Frontend: `ChatService` (Angular) + `ChatPanelComponent` (drawer, standalone, signals) integrado en `app.component.ts` junto al toggle del sidebar.
8. Infra: retirar servicio `open-webui` y volumen `openwebui_data` de `docker-compose.yml`; retirar la sección del cliente `cartera-openwebui` de `infra/KEYCLOAK.md`. `litellm` no cambia.
9. Validación manual end-to-end (conversación real con al menos un tool call) + tests E2E de Playwright si el panel lo justifica.

No hay estrategia de rollback de datos: al no existir usuarios reales del chat nativo antes de este change, no hay migración de conversaciones desde Open WebUI (su BD SQLite se descarta junto con el servicio).

## Open Questions

- ¿Se necesita un rate limit por usuario en `POST /api/chat/conversations/{id}/messages` para acotar coste de Bedrock, ahora que ya no existe la política `"agent"` del Tool Server? (Se puede resolver con `RequireRateLimiting` estándar de ASP.NET Core sobre el nuevo grupo si hace falta.)
- ¿El texto del system prompt necesita revisión por el negocio (tono, alcance) antes de producción, o se itera libremente en esta primera versión?
