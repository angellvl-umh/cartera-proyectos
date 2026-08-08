## Context

`ChatToolCatalog` (`Core/Features/Chat/Tools/`) es una lista plana de `ChatToolEntry` que cada una delega, vía `ISender`, en un `IRequest`/`IRequestHandler` de MediatR ya existente o en uno nuevo bajo `Core/Features/Agent/*`. El patrón actual mezcla dos estrategias:

1. **Wrapper `Agent*`**: para tools que necesitan resolver `personId` de forma explícita, aplicar autorización específica del agente, o recortar el DTO para no gastar contexto del LLM (p. ej. `AgentGetMyTasksQuery`, `AgentCreateProjectCommand`).
2. **Reuso directo**: para lecturas cuyo DTO ya es razonable, la tool podría llamar directamente al `IRequest` que ya usa el endpoint REST equivalente — pero hoy el catálogo no lo hace nunca; incluso las lecturas puramente informativas (`get_project_risks`, `get_project_dependencies`) pasan por un wrapper `Agent*Query` sin lógica adicional.

Al auditar qué le falta al catálogo frente al resto de la API (endpoints ya implementados: `SprintEndpoints`, `EpicEndpoints`, `TeamEndpoints` incl. `/activity`, `WorkItemEndpoints` incl. `/reorder` y `/bulk-sprint`, `ReportEndpoints` incl. `/velocity`, `/cycle-time`, `/portfolio/roadmap`, `/capacity/forecast`, `PromoterEndpoints`, `OrganicUnitEndpoints`, `TagEndpoints`) se confirma que **toda la lógica de negocio y de autorización ya existe** — no hay que crear dominio nuevo, solo exponerlo como tools.

También se confirma que la generación de Excel/gráficos de la integración anterior (Open WebUI + Python) desapareció por completo al migrar a chat nativo (commit `10badb5`), incluyendo el blob store HTTP (`AgentBlobStore`, in-memory sin TTL) y los endpoints públicos `/api/agent/charts/{id}` y `/api/agent/exports/{id}`.

## Goals / Non-Goals

**Goals:**
- Exponer como tools de chat las capacidades de dominio que ya existen en la API (sprints, épicas, equipos/actividad, backlog, roadmap, forecast, métricas, asignación equipo-proyecto, catálogos).
- Recuperar exportación a Excel y generación de gráficos, en .NET nativo, sin reintroducir un runtime Python.
- Mantener el criterio de "nunca duplicar lógica de negocio": las tools son adaptadores finos.
- Mejorar, no solo replicar, la infraestructura de blobs efímeros perdida (TTL real).

**Non-Goals:**
- No se corrigen huecos de autorización preexistentes que no forman parte de esta tarea (ver Riesgos: `CreateEpicCommand`/`UpdateEpicCommand` y `ReorderWorkItemsCommand`/`BulkAssignWorkItemsToSprintCommand` no comprueban pertenencia a equipo hoy; las tools nuevas se comportan igual que su endpoint REST equivalente, ni más ni menos permisivas).
- No se rediseña `ChatToolCatalog` ni el bucle de tool-calling de `SendChatMessage.cs`.
- No se añade edición de sprints/épicas más allá de lo pedido (update de sprint, delete, no se exponen como tool por ahora — bajo impacto conversacional, se pueden añadir después si se piden).
- No se implementa autenticación en los endpoints de servir chart/export: se mantiene el patrón de URL-capacidad no adivinable, igual que la versión anterior, ahora con expiración real.

## Decisions

### D1. Reutilizar `IRequest` existentes directamente cuando el DTO ya es apto para el LLM; envolver en `Agent*` solo cuando aporte algo
Para las tools de solo lectura nuevas (sprints, épicas, equipos/actividad, roadmap, forecast, velocity, cycle time, catálogos) se llama directamente, vía `ISender`, a los `IRequest` ya usados por los endpoints REST (`GetSprintsQuery`, `GetEpicsQuery`, `GetTeamActivityQuery`, `GetPortfolioRoadmapQuery`, `GetCapacityForecastQuery`, `GetProjectVelocityQuery`, `GetProjectCycleTimeQuery`, `GetPromotersQuery`, `GetOrganicUnitsQuery`, `GetTagsQuery`, `GetSprintBurndownQuery`) — sin crear un `Agent*Query` intermedio. Razón: esos DTOs ya son compactos (listas de pocos campos) y no hay recorte ni personId que resolver.

Para las tools de escritura (`create_sprint`, `activate_sprint`/`complete_sprint`, `create_epic`, `update_epic`, `reorder_backlog_item`, `bulk_assign_to_sprint`, `assign_project_team`) se llama también directamente a los comandos existentes (`CreateSprintCommand`, `TransitionSprintStatusCommand`, `CreateEpicCommand`, `UpdateEpicCommand`, `ReorderWorkItemsCommand`, `BulkAssignWorkItemsToSprintCommand`, `AssignTeamToProjectCommand`), pasando `personId` en `RequestingPersonId` donde el comando ya lo soporta. Ninguno de estos implementa `IAgentAuditable`, así que no quedan auditados en `AgentActionLog` — **decisión explícita**: se acepta este comportamiento porque ya es el que tienen hoy las tools de riesgos/dependencias que también son de escritura pero pasan por comandos que sí auditan; para mantener consistencia se envuelve cada comando de escritura nuevo en un `Agent*Command` fino (delega en `ISender` al comando real, implementa `IAgentAuditable`) — igual que `AgentTransitionProjectStatusCommand` ya hace hoy. Es decir: **lecturas → reuso directo; escrituras → wrapper `Agent*Command` solo para enganchar auditoría**, sin reimplementar reglas.

**Alternativa descartada**: envolver también las lecturas en `Agent*Query` "por consistencia" — se descarta por ser boilerplate puro sin beneficio (no hay personId que ocultar ni DTO que recortar en estos casos, y `IAgentAuditable` no aplica a queries).

### D2. Excel con ClosedXML, ejecutado sync dentro del handler
`ClosedXML` (MIT) genera el `.xlsx` en memoria (`XLWorkbook` → `MemoryStream`) de forma síncrona (no hay I/O real, es construcción de un DOM en memoria) dentro del propio `Handle` del comando. Mismo criterio de columnas que la versión Python (cabecera en negrita, autoancho). Se descarta EPPlus por su licencia comercial (Polyform Noncommercial) desde la v5, incompatible con "sin coste de licencia".

### D3. Gráficos como SVG construido a mano (sin librería)
Un helper interno `Core/Features/Chat/Tools/Charts/SvgChartBuilder.cs` construye el XML del SVG directamente (barras horizontales/verticales: `<rect>`; tarta/donut: `<path>` con arco calculado trigonométricamente — mismo enfoque que ya usa el frontend en `shared/charts/bar-chart` y `line-chart`, solo que aquí se genera en el backend como string). Devuelve `image/svg+xml`. Se descarta cualquier librería de gráficos (SkiaSharp, QuestPDF, etc.) por peso de dependencia innecesario para geometría simple.

### D4. Blob store efímero con `IMemoryCache`
Nuevo servicio `IEphemeralBlobStore` (interfaz en `Core/Interfaces`, implementación `MemoryCacheBlobStore` en `Infrastructure/Services`) que envuelve `IMemoryCache` con `SlidingExpiration` de 20 minutos. Guarda `(byte[] Data, string ContentType, string? FileName)` bajo un `Guid` como clave. Dos endpoints públicos nuevos en `Api/Endpoints/ChatBlobEndpoints.cs`:
- `GET /api/chat/charts/{id}` → `image/svg+xml`
- `GET /api/chat/exports/{id}` → `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet` con `Content-Disposition` del nombre de fichero

Ambos fuera de `RequireAuthorization()` (igual que la versión anterior) porque el propio chat, autenticado, es quien genera y entrega el enlace al usuario dentro de la conversación — el ID de 128 bits no es enumerable. Sin el mecanismo `Agent:ApiKey` de la integración Open WebUI (ya no aplica: no hay servicio externo llamando a estos endpoints, el propio backend los genera y los sirve al navegador del mismo usuario autenticado que ya tiene la respuesta en su chat).

### D5. Formato de retorno de las tools de export/chart
La tool NO devuelve el binario ni base64 al modelo (gastaría contexto y estas tools no lo necesitan interpretar). Devuelve `{ url, message }` con una URL absoluta construida desde `IConfiguration`/`IHttpContextAccessor` (mismo patrón que `Agent:ExternalUrl` de antes, revisar si sigue configurado o hay que añadirlo). El modelo compone el mensaje final incluyendo el link markdown (`[archivo.xlsx](url)` o `![gráfico](url)`) — se ajusta el prompt en `ChatSystemPrompt.Base` para indicar explícitamente que debe presentar la URL como link/imagen markdown y no reescribirla.

## Risks / Trade-offs

- **[Riesgo] `create_epic`/`update_epic`/`reorder_backlog_item`/`bulk_assign_to_sprint` heredan la falta de comprobación de pertenencia a equipo que ya tiene su endpoint REST** → Mitigación: documentarlo en la descripción de la tool para que quien revise sepa que es un gap preexistente, no introducido aquí; no se amplía el alcance de este change para arreglarlo (se puede proponer un change de hardening aparte).
- **[Riesgo] Endpoints públicos sin auth para servir blobs, aunque con TTL, siguen siendo accesibles por cualquiera que obtenga la URL** → Mitigación: TTL corto (20 min), ID no adivinable (Guid), sin listado ni enumeración posible; igual nivel de exposición que un link de "compartir" típico.
- **[Riesgo] Angular sanitiza `[innerHTML]` con el sanitizer por defecto; si bloquea `<img>` los gráficos no se verían** → Mitigación: verificar en implementación; si bloquea, es un ajuste mínimo y localizado en `chat-panel.component.ts` (usar `DomSanitizer.bypassSecurityTrustHtml` solo para el HTML ya generado por `marked`, que es contenido nuestro, no del usuario final).
- **[Trade-off] Nueva dependencia `ClosedXML`** → aceptado, es MIT y su huella es pequeña; alternativa de no tener Excel se descartó porque el usuario pidió explícitamente recuperar esa capacidad.
- **[Trade-off] SVG hecho a mano es más laborioso que una librería de charts, pero coherente con la convención ya establecida en el frontend** ("Gráficos SVG propios (sin librería de charts)") y evita una dependencia de render de imágenes (p. ej. SkiaSharp) solo para 5 tipos de gráfico simples.

## Migration Plan

No hay migración de datos (no se toca el modelo EF Core). Pasos de despliegue:
1. Añadir `ClosedXML` al `.csproj` de Infrastructure.
2. Registrar `IEphemeralBlobStore` (`IMemoryCache` ya viene con `AddMemoryCache()` si no está registrado — comprobar en `Program.cs`).
3. Añadir las nuevas particiones de `ChatToolCatalog` y registrarlas en `All()`.
4. Añadir `ChatBlobEndpoints` y mapearlos en `Program.cs`.
5. Actualizar `ChatSystemPrompt.Base`.
6. Sin rollback especial: si algo falla, es código nuevo aislado (nuevas tools no invocadas hasta que el LLM las use); revertir el commit basta.

## Open Questions

- ¿Se necesita `Agent:ExternalUrl` (o equivalente) en `appsettings.json` para construir URLs absolutas de descarga, o ya existe una config de base URL reutilizable en el backend? A resolver durante la implementación revisando `appsettings.json`/`Program.cs`.
- ¿Se exponen también `update_sprint`/`delete_sprint`/`delete_epic` como tools? Se dejan fuera por ahora (bajo valor conversacional); si el usuario los pide después, se añaden con el mismo patrón.
