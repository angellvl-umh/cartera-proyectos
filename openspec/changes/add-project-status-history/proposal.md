## Why

`WorkItem` y `Sprint` ya tienen histórico de transiciones de estado (`WorkItemStatusHistory`, `SprintStatusHistory`: from, to, quién, cuándo), pero `Project` no. `TransitionProjectStatusHandler` (`src/CarteraProyectos.Core/Features/Projects/TransitionProjectStatus.cs`) aplica `project.TransitionTo(newStatus)` y hace `SaveChangesAsync` sin dejar rastro de quién cambió el estado ni cuándo. Con 9 estados operativos y transiciones frecuentes (`PlanningWithClient → WaitingForDevelopers → PlanningSprint → InSprint → ...`), gestores y jefes de equipo no tienen forma de auditar cómo llegó un proyecto a su estado actual, ni cuánto tiempo pasó en cada fase — algo que sí pueden ver hoy para sprints y tareas.

## What Changes

- Nueva entidad `ProjectStatusHistory` (mismo shape que `SprintStatusHistory`): `ProjectId`, `FromStatus` (nullable), `ToStatus`, `ChangedById`, `ChangedAt`.
- `TransitionProjectStatusHandler` registra una entrada de histórico en cada transición válida.
- `CreateProjectHandler` registra la entrada inicial (`FromStatus: null → ToStatus: Stopped`) al crear el proyecto, igual que `CreateSprint` hace con `SprintStatusHistory`.
- Nuevo endpoint `GET /api/projects/{id}/status-history` (paginado no aplica — es un histórico acotado por proyecto, igual que `GET /api/projects/{projectId}/sprints/{id}/status-history`).
- Frontend: modal de histórico en `project-detail` accesible desde la cabecera de estado del proyecto, reutilizando el patrón visual de `openSprintHistory`/`openWorkItemHistory` (tabla De/A/Quién/Cuándo).

## Capabilities

### New Capabilities
- `project-status-history`: histórico auditable de todas las transiciones de estado de un `Project`, consultable vía API y visible en el detalle del proyecto.

### Modified Capabilities
(ninguna — no cambia el comportamiento de la máquina de estados de `Project`, solo añade auditoría)

## Impact

- **Dominio**: nueva entidad `Core/Domain/ProjectStatusHistory.cs`
- **Backend**: `TransitionProjectStatus.cs` (añade registro de histórico), `CreateProject.cs` (añade registro inicial), nuevo `GetProjectStatusHistory.cs` (query + DTO), `IAppDbContext`/`AppDbContext` (nuevo `DbSet<ProjectStatusHistory>`), `ProjectEndpoints.cs` (nuevo endpoint), migración EF Core nueva
- **Tests backend**: unit tests del nuevo handler/query + ajuste de tests existentes de `TransitionProjectStatusHandler`/`CreateProjectHandler` que verifiquen el histórico
- **Frontend**: `projects.service.ts` (nuevo método + tipo `ProjectStatusHistoryEntry`), `project-detail.component.ts` (modal + signal + trigger)
- **Agente IA**: fuera de alcance de este change — no se expone vía `/api/agent/*` (se puede añadir después si se pide explícitamente)
