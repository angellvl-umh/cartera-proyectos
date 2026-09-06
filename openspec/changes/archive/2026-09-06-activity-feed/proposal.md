## Why

El gestor de cartera hoy no tiene ninguna vista transversal de "qué está pasando" en la plataforma: para saber si un proyecto avanzó, si alguien completó una tarea o dejó un comentario, tiene que entrar proyecto por proyecto. La información ya existe — `ProjectStatusHistory`, `WorkItemStatusHistory`, `Comment` y `ProjectWeeklyUpdate` (HU-IN-00) se persisten desde hace tiempo — pero no hay ninguna consulta que las agregue cronológicamente. HU-IN-03 (`docs/07-informes-seguimiento.md`) pide exactamente eso: un feed único, filtrable y paginado.

## What Changes

- Nueva consulta `GetActivityFeedQuery` que agrega, ordenadas cronológicamente en orden inverso (más reciente primero), cinco tipos de evento ya persistidos y sin ningún cambio en su modelo de escritura:
  - Cambios de estado de proyecto (`ProjectStatusHistory`, excluyendo la entrada de creación con `FromStatus = null`, que no es un "cambio").
  - Nuevas tareas (`WorkItemStatusHistory` con `FromStatus = null`).
  - Tareas completadas (`WorkItemStatusHistory` con `ToStatus = Done`).
  - Comentarios (`Comment`).
  - Actualizaciones semanales de avance (`ProjectWeeklyUpdate`).
- Filtros combinables: por proyecto, por equipo (proyectos con ese equipo asignado en `ProjectTeamAssignment`) y por persona (autor/actor del evento).
- Paginación con el mismo contrato ya usado en el resto de la app (`PagedResult<T>`, `Page`/`PageSize`, tope de 100 por página).
- Nuevo endpoint `GET /api/activity` (autenticado, sin restricción de rol adicional — mismo patrón que el resto de `/api/reports`/`/api/portfolio`).
- Nueva pantalla Angular en `features/reports/` que lista el feed con icono/color por tipo de evento, filtros de proyecto/equipo/persona y paginación, enlazada desde el menú de informes.

## Capabilities

### New Capabilities
- `activity-feed`: consulta agregada y paginada de actividad reciente de la plataforma (cambios de estado, tareas nuevas/completadas, comentarios, avances semanales), filtrable por proyecto/equipo/persona, con su vista en el frontend.

### Modified Capabilities
(ninguna — no se modifica el modelo de escritura de `ProjectStatusHistory`, `WorkItemStatusHistory`, `Comment` ni `ProjectWeeklyUpdate`, solo se leen)

## Impact

- **Backend**: nuevo `Core/Features/Activity/GetActivityFeedQuery.cs` (+handler); nuevo endpoint en `Api/Endpoints/ReportEndpoints.cs` (o un `ActivityEndpoints.cs` si se prefiere separar — a decidir en design.md); no hay migración EF Core (no hay entidades nuevas).
- **Frontend**: nuevo componente `features/reports/activity-feed.component.ts`, entrada en `app.routes.ts` y enlace desde donde ya se navega a los demás informes.
- **Tests**: tests unitarios del handler (mezcla de tipos, orden cronológico, cada filtro por separado y combinado, paginación) y del endpoint (401 sin auth).
