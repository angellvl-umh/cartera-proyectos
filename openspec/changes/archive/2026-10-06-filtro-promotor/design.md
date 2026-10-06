## Context

`GetProjectsQuery` ya tiene `PromoterId` y el endpoint ya lee `promoterId` de la query (ver `ProjectEndpoints.cs`). `ProjectsService.getProjects` ya envía `filters.promoterId` y `ProjectsService.getPromoters()` devuelve hasta 100 promotores. Lo que falta es la UI. La vista Tablero (`KanbanByStatusComponent`) filtra en cliente sobre `/api/portfolio`, cuyo `PortfolioProjectDto` no incluye el promotor.

## Goals / Non-Goals

**Goals:** filtro por un promotor en Tabla y Tablero; tests del filtro en backend.

**Non-Goals:**
- Filtrar por varios promotores, o por "sin promotor".
- Ordenar por "Estado": el orden de la máquina de estados no es lineal y no hay un orden natural acordado.
- Ordenar el Tablero: sus columnas ya agrupan por estado.
- Que el Tablero respete el filtro "Estado": hoy no lo recibe, y su comportamiento esperado no está definido.

## Decisions

- **Tabla, filtro en servidor**: reutiliza `promoterId` de `GET /api/projects`. No hay cambios en `GetProjects`.
- **Tablero, filtro en cliente**: igual que hace hoy con texto y complejidad. Se añade `PromoterId` a `PortfolioProjectDto`, que es aditivo. Alternativa descartada: añadir `promoterId` como query de `/api/portfolio`. Duplicaría lógica, y las stats del portfolio dejarían de ser globales.
- **Selector**: `nz-select` con `nzShowSearch` y `nzAllowClear`, opciones de `ProjectsService.getPromoters()` cargadas una vez en el constructor, igual que las etiquetas. El estado vive en `filterPromoterId: number | null`, igual que el resto de filtros del componente.
- **Input del Tablero**: `@Input() filterPromoterId: number | null = null`, siguiendo el patrón de inputs existente del componente. No se migra a `input()` en este change.

- **Estado en la URL**: la URL es la fuente de verdad. Cada cambio de filtro, de página o de vista hace `router.navigate([], { relativeTo, queryParams, replaceUrl: false })` sin los parámetros vacíos o por defecto. El componente se suscribe a `ActivatedRoute.queryParamMap`: parsea y valida los valores (descarta los que no son números, los estados o complejidades que no están en los enums y las vistas desconocidas), actualiza los campos de filtro y carga. Así "atrás" y "recargar" funcionan sin código adicional, y no hay doble carga, porque los handlers solo navegan y la carga sale de la suscripción. Alternativa descartada: sincronizar con `Location.replaceState`, porque rompe el botón "atrás".
- **Columnas**: `ProjectListDto` añade `OrganicUnitId` y `OrganicUnitName` al final del record, y `GetProjects` hace `Include(p => p.OrganicUnit)`. El frontend añade los campos a la interfaz `Project` y muestra `promoterName ?? '—'` y `organicUnitName ?? '—'`.

- **Ordenación en servidor**: la paginación es en servidor, así que ordenar en cliente solo ordenaría la página actual. `GetProjectsQuery` añade `string? SortBy` y `string? SortDir` (strings, igual que el endpoint traduce hoy `status`/`complexity`: un valor desconocido se ignora y no da 400). En el handler, un `switch` sobre `sortBy` (normalizado a minúsculas) aplica `OrderBy(x => x.Campo == null).ThenBy(x => x.Campo)`, o `ThenByDescending`, para dejar los nulos al final, y después `ThenByDescending(p => p.Id)`. Para el texto se usa `.ToLower()`, que EF traduce a `lower()` en PostgreSQL. Para la complejidad se usa una expresión condicional enum → 0..4, que EF traduce a `CASE`, porque el enum se guarda como string y el orden alfabético no sirve. `ProjectEndpoints` añade los parámetros `sortBy` y `sortDir`, y su `WithDescription`.
- **UI de ordenación**: `nzSortFn`/`nzSortOrder` de NG-ZORRO con `[nzSortFn]="true"` (orden en servidor) y `(nzSortOrderChange)`, que navega con `sortBy`/`sortDir` y `page` omitido. `nzSortDirections` por defecto (`ascend`, `descend`, `null`).
- **Tamaño de página**: `nzShowSizeChanger` con `[nzPageSizeOptions]="[10, 20, 50, 100]"`. Ahora mismo la tabla no muestra ningún selector de tamaño, aunque `onPageSizeChange` existe.
- **Etiquetas en el Tablero**: `PortfolioProjectDto` añade `List<int> TagIds` al final, que es aditivo. `GetPortfolio` lo obtiene con una consulta aparte agrupada por proyecto, igual que hace hoy con `primaryTeams`, para no cargar las entidades `Tag` completas. El Tablero aplica `filterTagIds.length === 0 || p.tagIds.some(id => filterTagIds.includes(id))`.

## Risks / Trade-offs

- [Al cambiar un filtro de texto se escribe una entrada de historial por tecla] → el campo de texto navega con `replaceUrl: true`; el resto de filtros, la página y la vista crean entrada de historial.

- [Más de 100 promotores] → `getPromoters()` trae solo 100. Es aceptable con el volumen actual; si crece, se cambiará a búsqueda en servidor (`/api/promoters?q=`, que ya existe).
