## 1. Backend — eliminar SiptGroup

- [x] 1.1 `src/CarteraProyectos.Core/Domain/Project.cs`: eliminar el enum `SiptGroup`, el campo `Project.SiptGroup`, y el parámetro `siptGroup`/`SiptGroup` de `Project.Create` y `Project.Update`
- [x] 1.2 `src/CarteraProyectos.Core/Features/Projects/CreateProject.cs`: eliminar `SiptGroup` de `CreateProjectCommand` y de la llamada a `Project.Create`
- [x] 1.3 `src/CarteraProyectos.Core/Features/Projects/UpdateProject.cs`: eliminar `SiptGroup` de `UpdateProjectCommand` y de la llamada a `project.Update`
- [x] 1.4 `src/CarteraProyectos.Core/Features/Projects/GetProjects.cs`: eliminar `SiptGroup` de `GetProjectsQuery`, `ProjectListDto` y el filtro `p.SiptGroup == ...`
- [x] 1.5 `src/CarteraProyectos.Core/Features/Projects/GetProject.cs`: eliminar `SiptGroup` de `ProjectDetailDto` y de la proyección
- [x] 1.6 `src/CarteraProyectos.Core/Features/Reports/GetWeeklyPortfolioReport.cs`: eliminar `SiptGroup` de `GetWeeklyPortfolioReportQuery` y el filtro correspondiente (también se actualizó `ReportEndpoints.cs`, que expone esta query, no listado explícitamente en el plan original)
- [x] 1.7 `src/CarteraProyectos.Core/Features/Agent/AgentHandlers.cs`: eliminar `SiptGroup` de `AgentGetProjectsQuery` y su filtro
- [x] 1.8 `src/CarteraProyectos.Core/Features/Agent/AgentProjectsHandlers.cs`: eliminar el parseo y uso de `SiptGroup` en los comandos de crear/actualizar proyecto del agente
- [x] 1.9 `src/CarteraProyectos.Api/Endpoints/ProjectEndpoints.cs`: eliminar el query param `siptGroup` de `GET /api/projects` (parseo y paso a `GetProjectsQuery`) y su mención en `.WithDescription(...)`
- [x] 1.10 `src/CarteraProyectos.Api/Endpoints/AgentEndpoints.cs`: eliminar `siptGroup` de los query params de `get_projects`/informe semanal y de los records `AgentProjectCreateRequest`/`AgentProjectUpdateRequest`; actualizar las descripciones OpenAPI (`.WithDescription(...)`) para no mencionar `siptGroup` como campo válido
- [x] 1.11 `src/CarteraProyectos.Infrastructure/Persistence/AppDbContext.cs`: eliminar `e.Property(p => p.SiptGroup).HasConversion<string>();`
- [x] 1.12 Nueva migración EF Core: `RemoveProjectSiptGroup` (elimina la columna `SiptGroup` de `Projects`, sin backfill)
- [x] 1.13 `src/CarteraProyectos.Infrastructure/Persistence/DataSeeder.cs`: eliminar el `using static ... SiptGroup`, el parámetro `SiptGroup? sg` y su uso al construir proyectos seed
- [x] 1.14 `infra/seed.sql`: eliminar la columna `SiptGroup` del INSERT de proyectos; `infra/SEED.md`: quitar la mención a `SiptGroup` en la nota de enums-como-string

## 2. Backend — asignación de equipos en Create/Update

- [x] 2.1 `CreateProjectCommand`: añadir `IReadOnlyList<int>? TeamIds = null` y `int? PrimaryTeamId = null`; validator: si `PrimaryTeamId` tiene valor, debe estar contenido en `TeamIds` (si no, error de validación)
- [x] 2.2 `CreateProjectHandler`: tras el primer `SaveChangesAsync` (que asigna `project.Id`), si `TeamIds` tiene elementos, crear las filas `ProjectTeamAssignment` correspondientes (`IsPrimary = true` solo para `PrimaryTeamId`) y hacer un segundo `SaveChangesAsync`
- [x] 2.3 `UpdateProjectCommand`: mismos campos `TeamIds`/`PrimaryTeamId` que en 2.1, con la misma regla de validación
- [x] 2.4 `UpdateProjectHandler`: si `request.TeamIds` no es null, reemplazar completamente las `ProjectTeamAssignment` del proyecto (borrar las que ya no estén en `TeamIds`, crear las nuevas, aplicar `PrimaryTeamId`); si es null, no tocar la asignación de equipos existente

## 3. Frontend (usar opencode, no kiro-cli)

- [x] 3.1 `project.model.ts`: eliminar `SiptGroup`, `SIPT_GROUP_LABELS`, `siptGroup` de `Project`/`CreateProjectDto`/`ProjectFilters`; añadir `teamIds?: number[]` y `primaryTeamId?: number | null` a `CreateProjectDto`
- [x] 3.2 `project-form.component.ts`: sustituir el campo "Grupo SIPT" por un multi-select "Equipos" y un select "Equipo primario" (limitado a los equipos seleccionados, vía signal `computed`); `form`, `ngOnChanges` y `submit()` actualizados en consecuencia
- [x] 3.3 `project-detail.component.ts`: eliminado el descriptions-item de "Grupo SIPT"; añadido en la card "Equipos asignados" un control para añadir equipo (select con los equipos aún no asignados vía `assignableTeams` computed, switch "Primario" y botón "Asignar" que llama a `service.assignTeam(...)` y refresca la vista)
- [x] 3.4 `weekly-portfolio-report.component.ts`: eliminado el filtro "Grupo SIPT" (select hardcodeado, `filterSiptGroup`, query param `siptGroup`)

## 4. Tests

- [x] 4.1 `ProjectHandlerTests.cs`: adaptados los asserts sobre `SiptGroup`; añadidos tests de `CreateProjectHandler`/`UpdateProjectHandler` con `TeamIds`/`PrimaryTeamId` (crear con varios equipos y uno primario; sin TeamIds no crea asignaciones; editar reemplazando equipos; validación en create y en update cuando `PrimaryTeamId` no está en `TeamIds`; editar sin `TeamIds` no toca las asignaciones existentes)
- [x] 4.2 `AgentProjectsHandlerTests.cs`: eliminados los asserts sobre `SiptGroup`
- [x] 4.3 `GetWeeklyPortfolioReportHandlerTests.cs`: eliminado el test/asserts del filtro por `SiptGroup`

## 5. Documentación

- [x] 5.1 `docs/03-gestion-proyectos.md`: quitado el enum `SiptGroup` del modelo y de la tabla de valores; quitado `siptGroup` de la lista de filtros combinables
- [x] 5.2 `.ai/AGENTS.md`: quitado `SiptGroup?` de la fila de campos clave de `Project`
- [x] 5.3 `.ai/skills/domain/SKILL.md`: quitada la fila de `SiptGroup`
- [x] 5.4 (no planificado inicialmente) `docs/10-integracion-agente-ia.md`: quitadas dos menciones residuales a `siptGroup` en los criterios de aceptación de `get_projects`/`export_projects_excel`, detectadas en un grep final de verificación
