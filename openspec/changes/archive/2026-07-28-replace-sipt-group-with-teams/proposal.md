## Why

El campo `Project.SiptGroup` (grupo SIPT: Web Transversal, RRHH, Académico, Sede, Observatorio, Investigación/Económico) ya no representa nada útil para la gestión de la cartera y el usuario ha pedido eliminarlo. En su lugar, necesita poder asignar a cada proyecto uno o más de los equipos de personas ya definidos en la aplicación — esto ya es posible a nivel de dominio y backend (`ProjectTeamAssignment`, endpoints `POST/DELETE /api/projects/{id}/teams`, ya usado para permisos y en varios informes), pero la UI de alta/edición de proyecto no lo expone: hoy la asignación de equipos solo puede hacerse tras crear el proyecto, llamada por llamada, y la pestaña de detalle solo permite quitar equipos, no añadirlos.

## What Changes

- **BREAKING**: se elimina el enum `SiptGroup` y el campo `Project.SiptGroup` (columna de BD incluida, sin backfill — el dato deja de tener sentido según lo indicado por el usuario). Se elimina de: `CreateProjectCommand`/`UpdateProjectCommand`, `GetProjectsQuery`/`GetProjectQuery`/`GetWeeklyPortfolioReportQuery` y sus DTOs, los query params correspondientes en `ProjectEndpoints`/`AgentEndpoints`, el formulario y detalle de proyecto en Angular, el filtro del informe semanal de cartera, el seeder (`DataSeeder`/`seed.sql`) y la documentación de dominio.
- `CreateProjectCommand`/`UpdateProjectCommand` aceptan `TeamIds` (lista de ids de equipo) y `PrimaryTeamId` (opcional, debe estar incluido en `TeamIds`) en el mismo punto donde hoy va `SiptGroup` — igual que ya ocurre con `TagIds`. Al crear, se asignan los equipos indicados tras persistir el proyecto (necesita su Id); al editar, se reemplaza el conjunto de equipos asignados por el enviado (mismo patrón de "reemplazo completo" que ya usa `TagIds`), delegando en la misma invariante de "un único equipo primario" que ya aplica `AssignTeamToProjectCommand`.
- Frontend: el formulario de alta/edición de proyecto (`project-form.component.ts`) sustituye el select de "Grupo SIPT" por un multi-select de "Equipos" y un select de "Equipo primario" (limitado a los equipos seleccionados).
- Frontend: la pestaña "Equipos asignados" del detalle de proyecto (`project-detail.component.ts`), que ya lista y permite quitar equipos, incorpora el control que falta para añadir un equipo (con opción de marcarlo primario) usando los métodos de `ProjectsService` ya existentes (`assignTeam`, `getTeams`) pero sin usar hasta ahora.

Fuera de alcance: no se añade una tool de agente IA para gestionar equipos de proyecto (el agente pierde acceso a `siptGroup` en `create_project`/`update_project`/`get_projects`/informe semanal, pero no gana una tool equivalente para equipos en este change); no se toca el filtro `teamId` (equipo único) ya existente en listados/informes, que es independiente de esta funcionalidad de asignación múltiple.

## Capabilities

### New Capabilities
- `project-team-assignment`: asignación de uno o más equipos a un proyecto (con un equipo primario opcional) desde el alta/edición del proyecto y desde su pantalla de detalle.

### Modified Capabilities
(ninguna — no existía spec previa de gestión de proyectos en `openspec/specs/`; la eliminación de `SiptGroup` se documenta en el proposal/impact, no como delta de una capability existente)

## Impact

- `src/CarteraProyectos.Core/Domain/Project.cs` (elimina enum `SiptGroup` y campo, actualiza `Create`/`Update`)
- `src/CarteraProyectos.Core/Features/Projects/{CreateProject,UpdateProject,GetProjects,GetProject}.cs`
- `src/CarteraProyectos.Core/Features/Reports/GetWeeklyPortfolioReport.cs`
- `src/CarteraProyectos.Core/Features/Agent/{AgentHandlers,AgentProjectsHandlers}.cs`
- `src/CarteraProyectos.Api/Endpoints/{ProjectEndpoints,AgentEndpoints}.cs`
- `src/CarteraProyectos.Infrastructure/Persistence/AppDbContext.cs` (quita el `HasConversion<string>()` de `SiptGroup`)
- Nueva migración EF Core (drop columna `SiptGroup`)
- `src/CarteraProyectos.Infrastructure/Persistence/DataSeeder.cs`, `infra/seed.sql`, `infra/SEED.md`
- `src/frontend/src/app/features/projects/project.model.ts` (quita `SiptGroup`/`siptGroup`, añade `teamIds`/`primaryTeamId` a `CreateProjectDto`)
- `src/frontend/src/app/features/projects/project-form/project-form.component.ts`
- `src/frontend/src/app/features/projects/project-detail/project-detail.component.ts`
- `src/frontend/src/app/features/projects/projects.service.ts` (sin cambios de firma, ya soporta lo necesario)
- `src/frontend/src/app/features/reports/weekly-portfolio-report.component.ts` (quita el filtro de Grupo SIPT)
- Tests: `ProjectHandlerTests.cs`, `AgentProjectsHandlerTests.cs`, `GetWeeklyPortfolioReportHandlerTests.cs`
- Docs: `docs/03-gestion-proyectos.md`, `.ai/AGENTS.md`, `.ai/skills/domain/SKILL.md`
