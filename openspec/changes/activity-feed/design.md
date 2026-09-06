## Context

Las cinco fuentes de eventos ya existen y ya se escriben hoy sin cambios necesarios: `ProjectStatusHistory`, `WorkItemStatusHistory` (incluida la entrada con `FromStatus = null` que ya se crea en `CreateWorkItem` para marcar la creación), `Comment`, `ProjectWeeklyUpdate`. Ninguna tiene una columna común ni una vista SQL que las una. `Comment` no tiene `ProjectId` propio — cuelga de `WorkItemId`, y el proyecto se obtiene vía `WorkItem.ProjectId`. Ver proposal.md - Why.

## Goals / Non-Goals

**Goals:**
- Agregar los 5 tipos de evento en un único resultado ordenado y paginado, con filtros por proyecto/equipo/persona, reutilizando `PagedResult<T>` (`Core/Common`) tal cual lo usa `GetOrganicUnitsQuery`.
- Cero cambios en el modelo de escritura de las 4 entidades origen.

**Non-Goals:**
- Sin tiempo real (WebSockets/SignalR): el feed se consulta bajo demanda, igual que el resto de `/api/reports`.
- Sin tabla de eventos ni event sourcing: se lee directamente de las tablas existentes.
- Sin retención/purga: fuera de alcance de esta HU.

## Decisions

**Agregación: 5 queries EF filtradas + merge en memoria, no `UNION` en SQL crudo.**
Cada fuente se consulta por separado con sus propios `Where` (proyecto/equipo/persona) empujados a SQL, ordenada por su timestamp descendente, y se le aplica `Take(page * pageSize)` — es una propiedad conocida de un merge de k listas ya ordenadas: para obtener los primeros K elementos del resultado combinado basta con los primeros K de cada fuente. Los 5 resultados (como máximo `5 × page × pageSize` filas, acotado porque `pageSize` ya tiene tope 100) se combinan en memoria, se ordenan por `OccurredAt` descendente y se aplica `Skip`/`Take` para la página pedida. El total para `PagedResult.Total` se calcula con 5 `CountAsync` (mismos filtros), sin traer filas.
Alternativa considerada: una vista SQL con `UNION ALL` de las 5 tablas. Se descarta por ahora — añade una migración de vista + SQL crudo fuera del patrón EF Core del resto del proyecto, para un volumen de datos (universidad, cartera de proyectos) que no lo justifica todavía. Revisar si el volumen real lo pide.

**DTO común `ActivityEventDto(string Type, DateTimeOffset OccurredAt, int ProjectId, string ProjectTitle, int ActorId, string ActorName, string Summary)`.**
`Type` ∈ `ProjectStatusChanged | WorkItemCreated | WorkItemCompleted | CommentAdded | WeeklyUpdateRegistered` (mismo estilo de string que `HealthStatus`/`Status` en el resto de DTOs de informes, no un enum — el frontend ya consume strings para estos campos). `Summary` es texto ya formateado por tipo (p. ej. `"De EnCurso a Bloqueado"`, el texto del comentario truncado a ~140 car., o el `Summary` del avance semanal) — se arma en el handler, no en el frontend, para no duplicar lógica de formato en dos sitios.
`OccurredAt` normaliza los `DateTime` (naive, siempre UTC por convención ya existente: `DateTime.UtcNow` en todas las entidades origen) a `DateTimeOffset` con offset cero en el punto de lectura — no se tocan las entidades.

**Filtro "por persona" = actor del evento** (`ChangedById`/`AuthorId`), no "proyectos donde participa". Es la lectura literal de HU-IN-03 ("filtrable por... persona") y la más simple de razonar: "qué hizo esta persona".

**Filtro "por equipo" = proyectos con ese equipo asignado** (`ProjectTeamAssignment.TeamId == teamId`, sin distinguir `IsPrimary`), reutilizando la tabla que ya usa `GetWeeklyPortfolioReportQuery` para su propio filtro por equipo.

**Endpoint en `Api/Endpoints/ReportEndpoints.cs`, no un fichero nuevo.**
Ese fichero ya agrupa todos los endpoints de lectura/agregación de informes (`/api/portfolio`, `/api/capacity`, `/api/reports/weekly-portfolio`, `/api/projects/{id}/velocity`, `/api/projects/{id}/cycle-time`, `/api/portfolio/roadmap`, `/api/capacity/forecast`) — `/api/activity` sigue el mismo patrón y `RequireAuthorization()` sin rol adicional, igual que sus vecinos.

## Risks / Trade-offs

- [Riesgo] `Take(page * pageSize)` por fuente crece con `page`, así que páginas muy alejadas (scroll profundo) piden más filas de cada tabla → Mitigación: `pageSize` ya tiene tope 100 (mismo límite que `GetOrganicUnitsQuery`); el feed no está pensado para paginar cientos de páginas atrás. Si hace falta, una iteración futura puede añadir un filtro de rango de fechas para acotar antes de paginar.
- [Riesgo] Sin índice en las columnas de fecha de las tablas origen (`ChangedAt`/`CreatedAt`) bajo volumen alto → Mitigación: fuera de alcance (las tablas existen hoy sin ese índice); revisar si el volumen real de la universidad lo justifica.

## Migration Plan

Puramente aditivo: nueva query + nuevo endpoint + nueva pantalla, sin migración EF Core (no hay entidades nuevas) y sin tocar las 4 entidades origen. Rollback trivial: revertir el commit, no hay estado persistido que limpiar.
