## 1. Backend

- [x] 1.1 Añadir `int? PromoterId` a `PortfolioProjectDto` en `src/CarteraProyectos.Core/Features/Reports/GetPortfolio.cs` y rellenarlo en la proyección
- [x] 1.2 Añadir `int? OrganicUnitId` y `string? OrganicUnitName` al final de `ProjectListDto` en `GetProjects.cs`, con `Include(p => p.OrganicUnit)` y la proyección
- [x] 1.3 Ordenación en `GetProjectsQuery`/`GetProjectsHandler` (`SortBy`, `SortDir`; title/promoter/organicUnit/complexity/portfolioYear, nulos al final, desempate por `Id` descendente, valores desconocidos → orden por defecto) y parámetros `sortBy`/`sortDir` en `ProjectEndpoints` (con su `WithDescription`)
- [x] 1.4 Añadir `List<int> TagIds` al final de `PortfolioProjectDto` y rellenarlo en `GetPortfolio` (lista vacía si no hay etiquetas)
- [x] 1.5 Tests unitarios de `GetProjectsHandler` con `PromoterId`: filtra por promotor, excluye proyectos sin promotor, devuelve vacío si el id no existe, se combina con `Status`, y `Total` y la paginación son correctos
- [x] 1.6 Test unitario de `GetProjectsHandler`: `OrganicUnitId` y `OrganicUnitName` rellenos cuando hay unidad orgánica y null cuando no la hay
- [x] 1.7 Tests unitarios de ordenación: cada `sortBy` asc/desc, nulos al final, orden natural de la complejidad, desempate estable, ordenación entre páginas, valores desconocidos y combinación con filtros
- [x] 1.8 Tests unitarios de `GetPortfolio`: `PromoterId` presente o null, y `TagIds` con etiquetas o vacío

## 2. Frontend

- [x] 2.1 `projects-list.component.ts`: selector "Promotor" (`nz-select`, búsqueda, borrable, ~200px) tras "Etiquetas"; cargar promotores con `ProjectsService.getPromoters()` y tolerar errores; `filterPromoterId` en `buildFilters()` → `promoterId`; `applyFilters()` al cambiarlo
- [x] 2.2 `kanban-by-status.component.ts`: añadir `promoterId` y `tagIds` a su `PortfolioProjectDto` local y un `@Input() filterPromoterId`; filtrar en `filteredProjects` por promotor y por etiquetas (alguna de las seleccionadas); pasar `filterPromoterId` desde `projects-list`
- [x] 2.3 `project.model.ts`: añadir `organicUnitId: number | null` y `organicUnitName: string | null` a `Project`; en la tabla, columnas "Promotor" y "Unidad orgánica" (`?? '—'`) tras "Título"
- [x] 2.4 Tabla: columnas ordenables (Título, Promotor, Unidad orgánica, Complejidad, Año cartera) con orden en servidor (`nzSortFn` true + `nzSortOrderChange`), una sola columna a la vez; selector de tamaño de página (`nzShowSizeChanger`, 10/20/50/100); `ProjectFilters` y `ProjectsService.getProjects` envían `sortBy`/`sortDir`
- [x] 2.5 Estado en la URL en `projects-list`: `queryParamMap` → parseo y validación → campos de filtro + `currentPage` + `pageSize` + orden + `viewMode` → `loadProjects()`; los handlers de filtros, página y vista solo navegan (`router.navigate([], { relativeTo, queryParams })`, omitiendo vacíos y valores por defecto; el texto con `replaceUrl: true`)
- [x] 2.6 Tests Vitest de la función de parseo y serialización de query params: valores válidos, inválidos, vacíos y por defecto (page 1, pageSize 20, sin orden, `sortDir` sin `sortBy`), y `tagIds` repetidos
- [x] 2.7 Build del frontend (`pnpm build`) y tests (`pnpm exec vitest run`) en verde
