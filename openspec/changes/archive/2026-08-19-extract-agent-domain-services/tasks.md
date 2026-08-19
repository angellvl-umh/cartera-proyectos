## 1. WorkItems — `IWorkItemLifecycleService`

- [x] 1.1 Crear `Features/WorkItems/WorkItemLifecycleService.cs` con `IWorkItemLifecycleService` (`TransitionStatusAsync`, `ReorderAsync`, `BulkAssignToSprintAsync`), moviendo el cuerpo de `TransitionWorkItemStatusHandler.Handle`, `ReorderWorkItemsHandler.Handle`, `BulkAssignWorkItemsToSprintHandler.Handle` (`Features/WorkItems/*.cs`).
- [x] 1.2 Registrar `IWorkItemLifecycleService` en DI (`Program.cs`, mismo scope que `IAppDbContext`).
- [x] 1.3 `TransitionWorkItemStatusHandler`, `ReorderWorkItemsHandler`, `BulkAssignWorkItemsToSprintHandler` pasan a inyectar el servicio y delegar (adaptador fino, ya no contienen la lógica).
- [x] 1.4 `AgentUpdateTaskStatusHandler`, `AgentReorderBacklogHandler`, `AgentBulkAssignToSprintHandler` (`Features/Agent/AgentHandlers.cs`, `AgentBacklogHandlers.cs`) dejan de inyectar `ISender`; inyectan `IWorkItemLifecycleService` y llaman directamente.
- [x] 1.5 Actualizar los tests unitarios existentes de estos 6 handlers (mock de `ISender` → mock/fake de `IWorkItemLifecycleService`, o test directo del servicio).
- [x] 1.6 `dotnet build` + `dotnet test` sobre `CarteraProyectos.Core`/`UnitTests` limpio antes de pasar a la capa 2.

## 2. Projects — `IProjectLifecycleService`

- [x] 2.1 Crear `Features/Projects/ProjectLifecycleService.cs` con `IProjectLifecycleService` (`CreateAsync`, `UpdateAsync`, `TransitionStatusAsync`, `AssignTeamAsync`), moviendo el cuerpo de `CreateProjectHandler`, `UpdateProjectHandler`, `TransitionProjectStatusHandler`, `AssignTeamToProjectHandler`.
- [x] 2.2 Registrar en DI.
- [x] 2.3 Los 4 handlers de dominio pasan a ser adaptadores finos sobre el servicio.
- [x] 2.4 `AgentCreateProjectHandler`, `AgentUpdateProjectHandler`, `AgentTransitionProjectStatusHandler`, `AgentAssignProjectTeamHandler` (`Features/Agent/AgentProjectsHandlers.cs`, `AgentGovernanceHandlers.cs`) dejan `ISender` y llaman al servicio. Ojo: `AgentUpdateProjectHandler` hoy también inyecta `IAppDbContext` para resolver valores actuales antes de construir el comando — mantener esa parte (es normalización de input propia del adaptador, no lógica de dominio) y pasar el resultado ya resuelto al servicio.
- [x] 2.5 Actualizar tests unitarios de los 8 handlers afectados.
- [x] 2.6 Build + tests antes de pasar a la capa 3.

## 3. Projects — `IProjectGovernanceService` (riesgos y dependencias)

- [x] 3.1 Crear `Features/Projects/ProjectGovernanceService.cs` con `IProjectGovernanceService` (`GetRisksAsync`, `AddRiskAsync`, `UpdateRiskAsync`, `GetDependenciesAsync`, `AddDependencyAsync`), moviendo el cuerpo de `GetProjectRisksHandler`, `CreateProjectRiskHandler`, `UpdateProjectRiskHandler`, `GetProjectDependenciesHandler`, `CreateProjectDependencyHandler`.
- [x] 3.2 Registrar en DI.
- [x] 3.3 Los 5 handlers de dominio pasan a ser adaptadores finos.
- [x] 3.4 Los 5 `Agent*Handler` equivalentes en `Features/Agent/AgentGovernanceHandlers.cs` dejan `ISender` y llaman al servicio.
- [x] 3.5 Actualizar tests unitarios de los 10 handlers afectados.
- [x] 3.6 Build + tests antes de pasar a la capa 4.

## 4. Sprints — `ISprintLifecycleService`

- [x] 4.1 Crear `Features/Sprints/SprintLifecycleService.cs` con `ISprintLifecycleService` (`CreateAsync`, `TransitionStatusAsync` — este último cubre tanto activar como completar, incluida la lógica de `CarryOverTarget`/`TargetSprintId`), moviendo el cuerpo de `CreateSprintHandler` y `TransitionSprintStatusHandler`.
- [x] 4.2 Registrar en DI.
- [x] 4.3 `CreateSprintHandler`, `TransitionSprintStatusHandler` pasan a ser adaptadores finos.
- [x] 4.4 `AgentCreateSprintHandler`, `AgentActivateSprintHandler`, `AgentCompleteSprintHandler` (`Features/Agent/AgentSprintsEpicsHandlers.cs`) dejan `ISender` y llaman al servicio (conservan su propio parseo de `CarryOverTarget` desde string antes de llamar).
- [x] 4.5 Actualizar tests unitarios de los 5 handlers afectados.
- [x] 4.6 Build + tests antes de pasar a la capa 5.

## 5. Epics — `IEpicService`

- [x] 5.1 Crear `Features/Epics/EpicService.cs` con `IEpicService` (`CreateAsync`, `UpdateAsync`), moviendo el cuerpo de `CreateEpicHandler` y `UpdateEpicHandler`.
- [x] 5.2 Registrar en DI.
- [x] 5.3 Los 2 handlers de dominio pasan a ser adaptadores finos.
- [x] 5.4 `AgentCreateEpicHandler`, `AgentUpdateEpicHandler` (`Features/Agent/AgentSprintsEpicsHandlers.cs`) dejan `ISender` y llaman al servicio.
- [x] 5.5 Actualizar tests unitarios de los 4 handlers afectados.
- [x] 5.6 Build + tests antes de pasar a la capa 6.

## 6. Persons — `IPersonManagementService`

- [x] 6.1 Crear `Features/Persons/PersonManagementService.cs` con `IPersonManagementService` (`CreateAsync`, `GetListAsync`, `UpdateAsync`, `SetActiveAsync`), moviendo el cuerpo de `CreatePersonHandler`, `GetPersonsHandler`, `UpdatePersonHandler`, `SetPersonActiveHandler`. Nota: `CreateAsync` no estaba en el alcance original de esta task — se detectó durante la implementación que `AgentCreatePersonHandler` también anidaba vía `ISender` y se cubrió en la misma capa.
- [x] 6.2 Registrar en DI.
- [x] 6.3 Los 4 handlers de dominio pasan a ser adaptadores finos.
- [x] 6.4 `AgentGetPersonsHandler`, `AgentCreatePersonHandler`, `AgentUpdatePersonHandler`, `AgentSetPersonActiveHandler` (`Features/Agent/AgentPersonsHandlers.cs`) dejan `ISender` y llaman al servicio.
- [x] 6.5 Actualizar tests unitarios de los 8 handlers afectados.
- [x] 6.6 Build + tests antes de pasar a la capa 7.

## 7. Agent — servicios de lectura compartidos (Charts / Exports)

- [x] 7.1 Crear `Features/Agent/AgentReadServices.cs` (un único fichero con las 3 interfaces + implementaciones), moviendo el cuerpo de `AgentGetCapacityHandler`, `AgentGetProjectsHandler`, `AgentGetMyTasksHandler` (`Features/Agent/AgentHandlers.cs`) a `ICapacityReadService.GetAsync()`, `IProjectsReadService.GetAsync(personId, status)`, `IMyTasksReadService.GetAsync(personId)`. Extra detectado durante la implementación (fuera del alcance original): `AgentExportWeeklyReportExcelHandler` anidaba en `GetWeeklyPortfolioReportQuery` (query de dominio de `Features/Reports/`, no de Agent) — se cubrió con `IWeeklyPortfolioReportService` en el mismo fichero `GetWeeklyPortfolioReport.cs`.
- [x] 7.2 Registrar en DI (los 3 read services de Agent + `IWeeklyPortfolioReportService`).
- [x] 7.3 `AgentGetCapacityHandler`, `AgentGetProjectsHandler`, `AgentGetMyTasksHandler`, `GetWeeklyPortfolioReportHandler` pasan a ser adaptadores finos sobre estos servicios.
- [x] 7.4 Los 5 `AgentChart*Handler` (`Features/Agent/AgentChartHandlers.cs`) y los 2 `AgentExport*Handler` (`Features/Agent/AgentExportHandlers.cs`) dejan `ISender` y llaman directamente a los servicios de lectura en vez de `sender.Send(AgentGetCapacityQuery/...)`.
- [x] 7.5 Actualizar tests unitarios de los 11 handlers afectados.
- [x] 7.6 Build + tests antes de pasar a la capa 8. Verificado además con `grep -rl ISender src/CarteraProyectos.Core --include="*.cs"`: solo quedan `Features/Chat/SendChatMessage.cs` (la excepción) y `Features/Chat/Tools/ChatToolCatalog.cs` (recibe `ISender` como parámetro, no lo inyecta en un handler) — ningún `Agent*Handler` ni handler de dominio depende ya de `ISender`.

## 8. Test de arquitectura

- [x] 8.1 Crear proyecto `tests/CarteraProyectos.ArchTests` (xUnit, referencia a `CarteraProyectos.Core`). No hay fichero `.sln` en el repo (cada `.csproj` se compila por separado), así que no aplica añadirlo a ninguna solución. Se amplió `allowedPaths` de `.kiro/agents/backend-dev.json` para incluir `tests/CarteraProyectos.ArchTests/**`.
- [x] 8.2 Implementar `NoNestedMediatorHandlersTests.Handlers_do_not_depend_on_ISender_or_IMediator`: reflexión sobre el ensamblado de `CarteraProyectos.Core`, enumera tipos que implementan `IRequestHandler<>`/`IRequestHandler<,>`, falla si algún constructor tiene un parámetro de tipo `MediatR.ISender`/`MediatR.IMediator`, con excepción explícita solo para `SendChatMessageHandler`.
- [x] 8.3 `dotnet build`/`dotnet test` sobre `tests/CarteraProyectos.ArchTests/CarteraProyectos.ArchTests.csproj`: 1/1 test en verde, sin necesidad de excepciones adicionales. `dotnet build`/`dotnet test` de la API y `UnitTests` también en verde (504/504) tras esta capa.

## 9. Documentación

- [x] 9.1 Añadir a `.ai/skills/dotnet10/SKILL.md` la convención: "los `IRequestHandler` no dependen de `ISender`/`IMediator`; la lógica compartida entre el handler de dominio (REST) y su equivalente `Agent*Handler` vive en un servicio de aplicación plano en `Features/<Feature>/`; única excepción: `SendChatMessageHandler`, verificado por `tests/CarteraProyectos.ArchTests`" — nueva sección "Servicios de aplicación compartidos" + línea en "Prohibiciones".
- [x] 9.2 Actualizar la sección "Estado actual" de `.ai/AGENTS.md` (fuente real; `AGENTS.md` en la raíz es symlink) con mención breve del refactor y los 7 servicios introducidos, sin repetir el detalle que ya vive en `.ai/skills/dotnet10/SKILL.md`.
