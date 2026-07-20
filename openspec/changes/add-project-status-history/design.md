## Context

`Project.TransitionTo(newStatus)` (`Core/Domain/Project.cs`) valida la máquina de estados de 9 estados operativos pero no dej rastro de auditoría. `WorkItem` y `Sprint` sí lo tienen vía `WorkItemStatusHistory`/`SprintStatusHistory`, creadas en el handler de creación (estado inicial) y en el handler de transición (cada cambio), expuestas por un endpoint `GET .../status-history` y mostradas en `project-detail.component.ts` con un modal de tabla. Este change replica exactamente ese patrón para `Project`, sin tocar la máquina de estados en sí.

## Goals / Non-Goals

**Goals:**
- Auditar toda transición de estado de un `Project`: quién, cuándo, de qué estado a qué estado
- Registrar también el estado inicial al crear el proyecto (coherente con `CreateSprint`)
- Exponerlo vía API y verlo en el detalle del proyecto, con la misma UX que ya existe para sprints/tareas

**Non-Goals:**
- No cambia `Project.TransitionTo` ni las reglas de la máquina de estados
- No añade el histórico al Tool Server del agente IA (`/api/agent/*`) — se puede pedir como change separado si hace falta
- No añade filtros/paginación al histórico (igual que `SprintStatusHistory`, es una lista acotada por proyecto — un proyecto no genera miles de transiciones)

## Decisions

- **Mismo shape que `SprintStatusHistory`**: `Id`, `ProjectId`, `FromStatus` (nullable — null en el registro inicial), `ToStatus`, `ChangedById` (FK a `Person`, `OnDelete(Restrict)`), `ChangedAt` (UTC). Método factory estático `Create(Project project, ProjectStatus? fromStatus, ProjectStatus toStatus, int changedById)`.
- **Dos puntos de escritura**, replicando `CreateSprint`/`TransitionSprintStatus`:
  1. `CreateProjectHandler`: tras `db.Projects.Add(project)`, añade `ProjectStatusHistory.Create(project, null, project.Status, request.RequestingPersonId)`.
  2. `TransitionProjectStatusHandler`: captura `var oldStatus = project.Status` antes de `project.TransitionTo(...)`, y tras la transición añade `ProjectStatusHistory.Create(project, oldStatus, request.NewStatus, request.RequestingPersonId)`.
- **Query**: `GetProjectStatusHistoryQuery(int ProjectId)` → `IReadOnlyList<ProjectStatusHistoryDto>`, ordenado por `ChangedAt` ascendente, mismo DTO shape que `SprintStatusHistoryDto` (`Id, FromStatus?, ToStatus, ChangedById, ChangedByName, ChangedAt`). Lanza `KeyNotFoundException` si el proyecto no existe.
- **Endpoint**: `GET /api/projects/{id}/status-history` en `ProjectEndpoints.cs`, mismo formato de try/catch que `GetSprintStatusHistory` en `SprintEndpoints.cs`. No requiere autorización de gestión de proyecto (es solo lectura, igual que el resto de endpoints GET del recurso) — `RequireAuthorization()` heredado del grupo basta.
- **DbSet**: `ProjectStatusHistories` en `IAppDbContext`/`AppDbContext`, misma config de `HasOne(h => h.ChangedBy).WithMany().HasForeignKey(h => h.ChangedById).OnDelete(DeleteBehavior.Restrict)` y `HasOne(h => h.Project).WithMany().HasForeignKey(h => h.ProjectId).OnDelete(DeleteBehavior.Cascade)` (igual que `SprintStatusHistory` respecto a `Sprint` — si se borra el proyecto, se borra su histórico).
- **Migración**: nueva migración EF Core `AddProjectStatusHistory` (tabla `ProjectStatusHistories`).
- **Frontend**: `projects.service.ts` añade `interface ProjectStatusHistoryEntry` (mismo shape que `SprintStatusHistoryEntry` en `sprint.service.ts`) y `getStatusHistory(projectId: number)`. `project-detail.component.ts` añade `projectHistoryModalVisible`, `projectHistory` signals + `openProjectHistory()`, y un botón/icono junto al `nz-tag` de estado del proyecto en la cabecera que abre el modal (misma tabla De/A/Quién/Cuándo que `sprintHistoryModal`, usando `PROJECT_STATUS_LABELS` para las etiquetas en vez de `SPRINT_STATUS_COLORS`).

## Risks / Trade-offs

- **Tests existentes que instancian `CreateProjectCommand`/`TransitionProjectStatusCommand` con `RequestingPersonId` no seteado o inexistente en BD (InMemory)** romperán si se usa Foreign Key estricta contra un `Person` que no existe en el fixture — igual riesgo que ya se aceptó con `SprintStatusHistory`. Mitigación: revisar y actualizar los tests de `CreateProjectHandlerTests`/`TransitionProjectStatusHandlerTests` para crear un `Person` real en el `DbContext` de prueba antes de invocar el comando (patrón ya usado en `SprintHandlerTests`).
- Volumen de escritura extra por transición es despreciable (una fila por cambio de estado de proyecto, no por tarea).
