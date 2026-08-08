## 1. Infraestructura compartida (blob store + dependencia Excel)

- [x] 1.1 Añadir el paquete NuGet `ClosedXML` a `CarteraProyectos.Infrastructure.csproj`
- [x] 1.2 Definir `IEphemeralBlobStore` en `Core/Interfaces` (`Store(byte[] data, string contentType, string? fileName) -> Guid`, `TryGet(Guid id) -> (byte[] Data, string ContentType, string? FileName)?`)
- [x] 1.3 Implementar `MemoryCacheBlobStore` en `Infrastructure/Services` sobre `IMemoryCache` con `SlidingExpiration` de 20 minutos; registrar `AddMemoryCache()` si no está ya registrado en `Program.cs`, y registrar `IEphemeralBlobStore` en DI
- [x] 1.4 Crear `Api/Endpoints/ChatBlobEndpoints.cs` con `GET /api/chat/charts/{id:guid}` (sirve `image/svg+xml`) y `GET /api/chat/exports/{id:guid}` (sirve el xlsx con `Content-Disposition` y nombre de fichero), ambos sin `RequireAuthorization()`, devolviendo 404 si el id no existe o expiró; mapearlos en `Program.cs`
- [x] 1.5 Revisar `appsettings.json`/`Program.cs` para confirmar cómo construir la URL absoluta pública del backend (reutilizar configuración existente o añadir una si no hay ninguna) y documentar la decisión en un comentario breve donde se use — resuelto con `IPublicUrlProvider` (Core/Infrastructure) leyendo `Chat:PublicBaseUrl`, reutilizando la misma variable `PUBLIC_URL` que ya usa Cors:Origins en docker-compose.yml (corrección aplicada directamente por Claude Code tras revisar el output de kiro: los helpers basados en HttpContext que generó kiro no eran invocables desde Core)

## 2. Tools de sprints y épicas

- [x] 2.1 `ChatToolCatalog.Sprints.cs`: `get_sprints` (reusa `GetSprintsQuery`), `get_sprint_burndown` (reusa `GetSprintBurndownQuery`)
- [x] 2.2 `ChatToolCatalog.Sprints.cs`: `create_sprint` (reusa `CreateSprintCommand`, `RequestingPersonId` = personId del JWT)
- [x] 2.3 `ChatToolCatalog.Sprints.cs`: `activate_sprint` y `complete_sprint` (ambas reusan `TransitionSprintStatusCommand`; `complete_sprint` acepta `carryOver`/`targetSprintId` opcionales)
- [x] 2.4 `ChatToolCatalog.Epics.cs`: `get_epics` (reusa `GetEpicsQuery`), `create_epic` y `update_epic` (reusan `CreateEpicCommand`/`UpdateEpicCommand`)
- [x] 2.5 Registrar las 6 tools nuevas en `ChatToolCatalog.All()`

## 3. Tools de equipos, backlog, roadmap, forecast, métricas y asignación equipo-proyecto

- [x] 3.1 `ChatToolCatalog.Teams.cs`: `get_teams` (reusa `GetTeamsQuery` si existe, o el query que use `TeamEndpoints` para el listado) y `get_team_activity` (reusa `GetTeamActivityQuery`)
- [x] 3.2 `ChatToolCatalog.Backlog.cs`: `reorder_backlog_item` (reusa `ReorderWorkItemsCommand`) y `bulk_assign_to_sprint` (reusa `BulkAssignWorkItemsToSprintCommand`)
- [x] 3.3 `ChatToolCatalog.Portfolio.cs`: `get_portfolio_roadmap` (reusa `GetPortfolioRoadmapQuery`) y `get_capacity_forecast` (reusa `GetCapacityForecastQuery`)
- [x] 3.4 `ChatToolCatalog.Portfolio.cs`: `get_project_velocity` (reusa `GetProjectVelocityQuery`) y `get_project_cycle_time` (reusa `GetProjectCycleTimeQuery`)
- [x] 3.5 Crear `AgentAssignProjectTeamCommand` en `Core/Features/Agent/AgentProjectsHandlers.cs` (implementa `IAgentAuditable`, delega vía `ISender` en `AssignTeamToProjectCommand`) y exponer `assign_project_team` en `ChatToolCatalog.Projects.cs`
- [x] 3.6 Registrar las 8 tools nuevas en `ChatToolCatalog.All()`

## 4. Tools de catálogos

- [x] 4.1 `ChatToolCatalog.Catalogs.cs`: `get_promoters` (reusa `GetPromotersQuery`), `get_organic_units` (reusa `GetOrganicUnitsQuery`), `get_tags` (reusa `GetTagsQuery`)
- [x] 4.2 Registrar las 3 tools nuevas en `ChatToolCatalog.All()`

## 5. Exportación a Excel

- [x] 5.1 Crear `Core/Features/Chat/Tools/Exports/ExcelExportBuilder.cs` con los dos builders (proyectos, informe semanal de cartera) usando `ClosedXML`: cabecera en negrita, autoancho de columna, mismas columnas que la versión Python de referencia (`git show 10badb5^:infra/open-webui/cartera_tool.py`, líneas 577-706)
- [x] 5.2 `ChatToolCatalog.Exports.cs`: `export_projects_excel` (reusa `AgentGetProjectsQuery`, construye el xlsx, lo guarda en `IEphemeralBlobStore`, devuelve `{ url, message }`); mensaje explícito si no hay proyectos que exportar
- [x] 5.3 `ChatToolCatalog.Exports.cs`: `export_weekly_portfolio_report_excel` (reusa `GetWeeklyPortfolioReportQuery`, proyectos en riesgo primero con columna "En riesgo")
- [x] 5.4 Registrar las 2 tools nuevas en `ChatToolCatalog.All()`

## 6. Gráficos SVG

- [x] 6.1 Crear `Core/Features/Chat/Tools/Charts/SvgChartBuilder.cs`: helpers para barras horizontales/verticales (`<rect>`) y tarta/donut (`<path>` con arco trigonométrico), con paleta de colores equivalente a la versión Python de referencia (verde/amarillo/rojo para carga, paleta categórica para estados/equipos)
- [x] 6.2 `ChatToolCatalog.Charts.cs`: `chart_team_capacity` (reusa `AgentGetCapacityQuery`), `chart_project_progress` (reusa `AgentGetProjectsQuery`)
- [x] 6.3 `ChatToolCatalog.Charts.cs`: `chart_my_tasks_by_status` (reusa `AgentGetMyTasksQuery`, `chartType`: donut por defecto o bar), `chart_projects_by_status` (reusa `AgentGetProjectsQuery`, `chartType`: pie por defecto o bar), `chart_projects_by_team` (reusa `AgentGetProjectsQuery`, `chartType`: bar por defecto o pie)
- [x] 6.4 Cada tool guarda el SVG en `IEphemeralBlobStore` y devuelve `{ url, message }` con el link de imagen markdown ya formado
- [x] 6.5 Registrar las 5 tools nuevas en `ChatToolCatalog.All()`

## 7. Frontend (solo si hace falta)

- [x] 7.1 Verificar en el navegador que un mensaje de assistant con `![gráfico](url)` se renderiza como `<img>` visible tras pasar por `marked.parse` + `[innerHTML]` en `chat-panel.component.ts` — verificado con un test Vitest que usa la función interna real de saneado de Angular (`ɵ_sanitizeHtml`) en vez de simularla; 3/3 tests OK (`src/frontend/src/app/features/chat/chat-panel.component.spec.ts`)
- [x] 7.2 Si el sanitizador de Angular elimina el `<img>`, ajustar `chat-panel.component.ts` — no hizo falta: Angular permite `<img src="https://...">` en `[innerHTML]` por defecto, componente sin cambios

## 8. System prompt y documentación

- [x] 8.1 Actualizar `ChatSystemPrompt.Base` para listar las nuevas capacidades (sprints, épicas, equipos, backlog, roadmap, forecast, métricas, catálogos, exportación a Excel, gráficos) y para indicar que las URLs devueltas por export/chart deben presentarse como link/imagen markdown tal cual, sin reescribirlas (hecho directamente por Claude Code, sin kiro — edición pequeña de un único fichero)
- [x] 8.2 Actualizar la sección "Estado actual" de `AGENTS.md` con el nuevo alcance del chat nativo (hecho directamente por Claude Code sobre `.ai/AGENTS.md`, destino real del symlink)

## 9. Tests

- [x] 9.1 Tests unitarios para cada tool nueva de solo lectura — decisión tomada durante la implementación: las tools de solo lectura del grupo 2/3/4 son delegación pura sin lógica añadida sobre queries ya testeadas (`tests/CarteraProyectos.UnitTests/Features/{Sprints,Epics,Teams,Reports,Promoters,OrganicUnits,Tags}/`); no se duplican esos tests, el esfuerzo se centró en el código nuevo con lógica propia (9.2-9.5)
- [x] 9.2 Tests unitarios para cada tool nueva de escritura (create/activate/complete sprint, create/update epic, reorder backlog, bulk assign to sprint, assign project team): happy path, not found, regla de negocio violada, permisos insuficientes — `AgentSprintsEpicsHandlerTests.cs`, `AgentBacklogHandlerTests.cs`, ampliación de `AgentProjectsHandlerTests.cs`
- [x] 9.3 Tests unitarios de `ExcelExportBuilder` (o de las tools que lo envuelven): xlsx generado con cabecera y filas esperadas; caso sin resultados no genera fichero — `ExcelExportBuilderTests.cs` + `AgentExportChartHandlerTests.cs`
- [x] 9.4 Tests unitarios de `SvgChartBuilder`/tools de gráficos: SVG válido generado con los valores esperados; caso sin datos no genera imagen — `SvgChartBuilderTests.cs` + `AgentExportChartHandlerTests.cs`
- [x] 9.5 Tests de `MemoryCacheBlobStore`: guardar y recuperar un blob; recuperar un id inexistente devuelve null — `MemoryCacheBlobStoreTests.cs` (expiración por tiempo real no se prueba, no merece la pena esperar 20 min reales en un test)
- [x] 9.6 Ejecutar la suite completa (`dotnet test`) y confirmar que no hay regresiones antes de dar la capa por terminada — 500/500 tests OK (verificado por Claude Code con el SDK de .NET dockerizado, ya que `dotnet` no está disponible ni en el entorno de Claude Code ni en el de los agentes kiro)
