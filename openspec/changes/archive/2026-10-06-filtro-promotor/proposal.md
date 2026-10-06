## Why

Los gestores necesitan ver de un vistazo los proyectos de un promotor concreto, por ejemplo para preparar una reunión con él. La API ya admite `promoterId` en `GET /api/projects`, pero el listado de proyectos no ofrece ese filtro, y la vista Tablero (`/api/portfolio`) ni siquiera expone el promotor de cada proyecto.

## What Changes

- Nuevo selector "Promotor" en la barra de filtros del listado de proyectos (`/projects`): un solo valor, con búsqueda y borrable. Se combina con los filtros existentes (texto, estado, complejidad y etiquetas).
- En la vista Tabla, el filtro se aplica en servidor con `promoterId` y la paginación vuelve a la página 1.
- En la vista Tablero, el filtro se aplica en cliente sobre los datos de `/api/portfolio`.
- `GET /api/portfolio` añade `promoterId` (nullable) a cada proyecto. Es un cambio aditivo y no rompe a los clientes actuales.
- Los filtros del listado (texto, estado, complejidad, etiquetas y promotor), la página y la vista (Tabla/Tablero) se guardan en la URL como query params. Una URL compartida o recargada reabre el listado con el mismo estado, y "atrás" del navegador vuelve al estado anterior.
- La tabla muestra dos columnas nuevas: "Promotor" y "Unidad orgánica". `GET /api/projects` añade `organicUnitId` y `organicUnitName` (nullable) a cada elemento; también es aditivo.
- Ordenación en servidor de la tabla por Título, Promotor, Unidad orgánica, Complejidad y Año cartera: `GET /api/projects` acepta `sortBy` y `sortDir`. El orden también se guarda en la URL.
- El tamaño de página se puede elegir en la tabla (10/20/50/100) y se guarda en la URL (`pageSize`).
- Corrección: el Tablero ignoraba el filtro de etiquetas. `GET /api/portfolio` añade `tagIds` a cada proyecto y el Tablero filtra por ellas con la misma semántica que la Tabla (el proyecto tiene alguna de las etiquetas seleccionadas).
- Tests unitarios de la ordenación, del filtro `promoterId` y de los campos de unidad orgánica en `GetProjects`, y de los nuevos campos de `GetPortfolio`.

## Capabilities

### New Capabilities
- `project-list-promoter-filter`: filtrar el listado de proyectos (Tabla y Tablero) por promotor, guardar los filtros en la URL mostrar promotor y unidad orgánica en la tabla, ordenar la tabla y elegir el tamaño de página; corrige el filtro de etiquetas del Tablero.

### Modified Capabilities
<!-- ninguna: no hay spec previa del listado de proyectos ni de /api/portfolio -->

## Impact

- Backend: `Core/Features/Reports/GetPortfolio.cs` (DTO + proyección), `Core/Features/Projects/GetProjects.cs` (`ProjectListDto`, `Include` de `OrganicUnit`, ordenación) y `Api/Endpoints/ProjectEndpoints.cs` (`sortBy`/`sortDir`).
- Frontend: `features/projects/projects-list/projects-list.component.ts`, `kanban-by-status/kanban-by-status.component.ts` y la interfaz `Project` de `project.model.ts`.
- Tests: `tests/CarteraProyectos.UnitTests/Features/Projects/` y `Features/Reports/`.
- Sin migraciones ni cambios de permisos.
