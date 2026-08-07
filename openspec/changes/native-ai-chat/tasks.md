## 1. Backend — Cliente de chat completion (LiteLLM vía SDK OpenAI)

- [x] 1.1 Añadir el paquete NuGet `OpenAI` a `CarteraProyectos.Infrastructure.csproj`.
- [x] 1.2 Definir en `Core/Interfaces` la abstracción `IChatCompletionClient` con DTOs propios (`ChatCompletionRequest`, `ChatCompletionResponse`, `ChatToolCall`, `ChatToolDefinition`) — sin referenciar el SDK `OpenAI` desde Core.
- [x] 1.3 Implementar `Infrastructure/Services/LiteLlmChatCompletionClient.cs` usando `ChatClient` del SDK `OpenAI` apuntando a la `base_url` de LiteLLM, traduciendo hacia/desde los DTOs de `IChatCompletionClient` (mismo patrón que `BedrockEmbeddingService`).
- [x] 1.4 Añadir configuración `LiteLlm:BaseUrl`, `LiteLlm:ApiKey`, `Chat:Model` en `appsettings.json` y registrar `IChatCompletionClient` en `Program.cs` (`AddSingleton`, igual que `IEmbeddingService`).
- [x] 1.5 Añadir las variables de entorno correspondientes al servicio `backend` en `docker-compose.yml` (`LiteLlm__BaseUrl=http://litellm:4000`, `LiteLlm__ApiKey=sk-cartera-litellm-key`, `Chat__Model=claude-sonnet` o el que se decida).

## 2. Backend — Dominio y persistencia de conversaciones

- [x] 2.1 Crear entidades `Conversation` (Id, PersonId, Title, CreatedAt, UpdatedAt) y `ChatMessage` (Id, ConversationId, Role, Content, ToolCallsJson?, ToolName?, ToolCallId?, CreatedAt) en `Core/Domain`, siguiendo el patrón `private set` + factory `Create` estático (ver `Comment.cs`/`AgentActionLog.cs`).
- [x] 2.2 Registrar los `DbSet<Conversation>`/`DbSet<ChatMessage>` en `IAppDbContext`/`AppDbContext` y su configuración EF Core (relaciones con `Person`, enum `Role` como string).
- [x] 2.3 Generar la migración EF Core (`AddChatConversations` o similar) y verificar que aplica limpio sobre la BD de desarrollo.

## 3. Backend — Catálogo de tools y bucle de tool-calling

- [x] 3.1 Crear `Core/Features/Chat/Tools/ChatToolCatalog.cs`: una entrada por cada tool hoy expuesta en `AgentEndpoints.cs` (excepto `store_chart`/`store_export`, que se eliminan sin reemplazo — ver design.md), reutilizando el mismo nombre, descripción y JSON Schema de parámetros que ya existe en `.WithName`/`.WithSummary`/`.WithDescription`.
- [x] 3.2 Cada entrada del catálogo debe construir el `Agent*Command`/`Query` correspondiente (los mismos tipos de `Core/Features/Agent/*Handlers.cs`, sin modificarlos) y enviarlo con `ISender.Send`, recibiendo `personId` como parámetro explícito de la función (nunca leído de los argumentos que envía el modelo).
- [x] 3.3 Redactar el system prompt base del asistente (constante/recurso en `Core/Features/Chat`, en español).
- [x] 3.4 Implementar `SendChatMessageCommand`/`SendChatMessageHandler`: persiste el mensaje de usuario, arma el historial + tools desde `ChatToolCatalog`, llama a `IChatCompletionClient`, ejecuta el bucle de tool-calling (tope de 5 iteraciones), persiste los mensajes intermedios (`Tool`) y el mensaje final del asistente, y devuelve la respuesta.
- [x] 3.5 Implementar los comandos/queries de gestión de conversaciones: `CreateConversationCommand`, `GetConversationsQuery` (paginado, propias del usuario), `GetConversationMessagesQuery` (404 si no es del usuario), `DeleteConversationCommand`.

## 4. Backend — Endpoints `/api/chat/*`

- [x] 4.1 Crear `Api/Endpoints/ChatEndpoints.cs` con `MapGroup("/api/chat")`, `RequireAuthorization()`, resolviendo la persona con `CurrentUser.ResolveAsync` (mismo patrón que `DashboardEndpoints`/`CommentEndpoints`).
- [x] 4.2 Mapear `POST /api/chat/conversations`, `GET /api/chat/conversations`, `GET /api/chat/conversations/{id}/messages`, `POST /api/chat/conversations/{id}/messages`, `DELETE /api/chat/conversations/{id}`.
- [x] 4.3 Registrar el grupo en `Program.cs` (descripciones OpenAPI en español, sin añadirlo al grupo `"agent"` que se elimina en la tarea 5).

## 5. Backend — Retirada del Tool Server `/api/agent/*`

- [x] 5.1 Eliminar `Api/Endpoints/AgentEndpoints.cs` (incluye `AgentApiKeyFilter`, `AgentBlobStore` y los endpoints de charts/exports).
- [x] 5.2 Eliminar el registro del grupo OpenAPI `"agent"` y la política de rate limiting `"agent"` en `Program.cs`.
- [x] 5.3 Revisar `Core/Features/Agent/*Handlers.cs`: mantener los comandos/queries y handlers tal cual (los reutiliza `ChatToolCatalog`); solo limpiar lo que quedara huérfano exclusivamente por el Tool Server (p. ej. DTOs de request usados solo por `AgentEndpoints.cs`).
- [x] 5.4 Actualizar `AGENTS.md` (sección "Agente IA (Open WebUI Tool Server)") para reflejar el nuevo chat nativo.

## 6. Frontend — Panel de chat

- [x] 6.1 Crear `ChatService` (Angular, `HttpClient`) con métodos `listConversations`, `createConversation`, `getMessages`, `sendMessage`, `deleteConversation`.
- [x] 6.2 Crear `ChatPanelComponent` (standalone, `OnPush`, signals, NG-ZORRO `nz-drawer`): lista de conversaciones, botón "nueva conversación", área de mensajes, input de envío, estado de carga mientras se resuelve la respuesta (sin streaming).
- [x] 6.3 Integrar el toggle del panel en `app.component.ts`, junto al botón de plegar el sidebar (icono en el header, accesible desde cualquier ruta).
- [x] 6.4 Manejar errores de la API (p. ej. tool rechazada por permisos) mostrando el mensaje del asistente o un `nz-message` de error según corresponda.

## 7. Infra — Retirada de Open WebUI

- [x] 7.1 Eliminar el servicio `open-webui` y el volumen `openwebui_data` de `docker-compose.yml`.
- [x] 7.2 Eliminar del `docker-compose.yml` la variable `OPENWEBUI_CLIENT_SECRET` si no se usa en ningún otro sitio.
- [x] 7.3 Actualizar `infra/KEYCLOAK.md`: eliminar la sección del cliente `cartera-openwebui` y cualquier referencia a Open WebUI en la documentación de SSO.
- [x] 7.4 Revisar `infra/seed.sql` y demás scripts de infra por si referencian Open WebUI.

## 8. Tests y validación

- [x] 8.1 Tests unitarios (xUnit + EF InMemory + Shouldly) de `SendChatMessageHandler`: turno sin tools, turno con una tool, turno que alcanza el tope de iteraciones, rechazo de una tool por falta de permisos.
- [x] 8.2 Tests unitarios de los endpoints de gestión de conversaciones: aislamiento entre usuarios (404 al intentar leer una conversación ajena).
- [x] 8.3 Verificar `dotnet build` y la suite completa de tests unitarios en verde.
- [x] 8.4 Levantar el stack (`docker compose up`, o el stack E2E efímero) y validar manualmente una conversación real con al menos un tool call de escritura y una de solo lectura.
- [x] 8.5 Verificar `ng build`/`tsc --noEmit` del frontend en verde.
