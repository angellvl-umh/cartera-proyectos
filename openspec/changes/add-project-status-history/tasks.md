## 1. Backend

- [x] 1.1 Crear `Core/Domain/ProjectStatusHistory.cs`: mismo shape que `Core/Domain/SprintStatusHistory.cs` (Id, ProjectId, FromStatus? ProjectStatus, ToStatus ProjectStatus, ChangedById, ChangedAt, navegación `Project?` y `ChangedBy?`), con factory estático `Create(Project project, ProjectStatus? fromStatus, ProjectStatus toStatus, int changedById)`
- [x] 1.2 Añadir `DbSet<ProjectStatusHistory> ProjectStatusHistories` a `Core/Interfaces/IAppDbContext.cs` y a `Infrastructure/Persistence/AppDbContext.cs`, con configuración EF Core: `HasOne(h => h.Project).WithMany().HasForeignKey(h => h.ProjectId).OnDelete(DeleteBehavior.Cascade)` y `HasOne(h => h.ChangedBy).WithMany().HasForeignKey(h => h.ChangedById).OnDelete(DeleteBehavior.Restrict)` (igual que la config existente de `SprintStatusHistory`)
- [x] 1.3 Generar migración EF Core `AddProjectStatusHistory` (`dotnet ef migrations add AddProjectStatusHistory --project src/CarteraProyectos.Infrastructure --startup-project src/CarteraProyectos.Api`)
- [x] 1.4 Modificar `Core/Features/Projects/CreateProject.cs`: tras `db.Projects.Add(project)`, añadir `db.ProjectStatusHistories.Add(ProjectStatusHistory.Create(project, null, project.Status, request.RequestingPersonId))` antes de `SaveChangesAsync`
- [x] 1.5 Modificar `Core/Features/Projects/TransitionProjectStatus.cs`: capturar `var oldStatus = project.Status` antes de `project.TransitionTo(request.NewStatus)`, y tras la transición añadir `db.ProjectStatusHistories.Add(ProjectStatusHistory.Create(project, oldStatus, request.NewStatus, request.RequestingPersonId))` antes de `SaveChangesAsync`
- [x] 1.6 Crear `Core/Features/Projects/GetProjectStatusHistory.cs`: `GetProjectStatusHistoryQuery(int ProjectId)` → `IReadOnlyList<ProjectStatusHistoryDto>`, `ProjectStatusHistoryDto(int Id, string? FromStatus, string ToStatus, int ChangedById, string ChangedByName, DateTime ChangedAt)`, handler que valida que el proyecto existe (`KeyNotFoundException` si no) y devuelve las entradas ordenadas por `ChangedAt` ascendente — mismo patrón que `Core/Features/Sprints/GetSprintStatusHistory.cs`
- [x] 1.7 Añadir endpoint `GET /{id:int}/status-history` en `Api/Endpoints/ProjectEndpoints.cs`, mismo patrón try/catch que el endpoint equivalente en `Api/Endpoints/SprintEndpoints.cs` (404 en `KeyNotFoundException`), con descripción OpenAPI en español

## 2. Frontend

- [x] 2.1 En `src/frontend/src/app/features/projects/projects.service.ts`: añadir `interface ProjectStatusHistoryEntry` (mismo shape que `SprintStatusHistoryEntry` en `sprint.service.ts`: `id, fromStatus: string | null, toStatus: string, changedById: number, changedByName: string, changedAt: string`) y método `getStatusHistory(projectId: number): Observable<ProjectStatusHistoryEntry[]>` que llama a `GET /api/projects/{id}/status-history`
- [x] 2.2 En `src/frontend/src/app/features/projects/project-detail/project-detail.component.ts`: añadir signals `projectHistoryModalVisible`, `projectHistory`, `projectHistoryLoading` y método `openProjectHistory()` que llama al servicio, siguiendo exactamente el patrón de `openSprintHistory()`
- [x] 2.3 En el template del componente: añadir un botón/icono junto al `nz-tag` de estado del proyecto en la cabecera (visible siempre que se pueda ver el detalle del proyecto) que invoque `openProjectHistory()`, y el modal `nz-modal` con la tabla De/A/Quién/Cuándo (mismo layout que `sprintHistoryModal`), usando `PROJECT_STATUS_LABELS` para mostrar los estados

## 3. Tests

- [x] 3.1 Actualizar/crear tests unitarios en `tests/CarteraProyectos.UnitTests/Features/Projects/` para `CreateProjectHandler`: verificar que se crea la entrada inicial de `ProjectStatusHistory` (`FromStatus == null`, `ToStatus == Stopped`)
- [x] 3.2 Actualizar/crear tests unitarios para `TransitionProjectStatusHandler`: verificar que cada transición válida registra la entrada de histórico correcta (`FromStatus`/`ToStatus`/`ChangedById`), y que una transición inválida NO crea entrada de histórico
- [x] 3.3 Crear tests unitarios para `GetProjectStatusHistoryHandler`: happy path (lista ordenada cronológicamente), proyecto no encontrado (`KeyNotFoundException`)
- [x] 3.4 Revisar los tests existentes de `CreateProjectHandlerTests`/`TransitionProjectStatusHandlerTests` que instancien sus commands sin un `Person` real en el `DbContext` InMemory — añadir el `Person` al fixture si la nueva FK de `ChangedById` lo requiere (mismo ajuste que ya se hizo para `SprintHandlerTests` al añadir `SprintStatusHistory`)
- [x] 3.5 `dotnet build src/` y `dotnet test` en verde
