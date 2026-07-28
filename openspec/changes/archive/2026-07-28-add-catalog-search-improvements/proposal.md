## Why

El catálogo de Unidades Orgánicas ya soporta búsqueda por texto en el backend (`GetOrganicUnitsQuery`/`CatalogsService.getOrganicUnits`) pero el listado admin (`organic-units-list.component.ts`) no expone ninguna caja de búsqueda — quedó fuera de alcance en el change anterior (`add-promoters-search`). Además, la búsqueda por nombre en ambos catálogos (Promotores y Unidades Orgánicas) usa `Name.Contains(q)`, que Npgsql traduce a `LIKE` case-sensitive y no pliega acentos/diacríticos: buscar "promocion" no encuentra "Promoción" y "uni" no encuentra "Unidad" si la capitalización no coincide exactamente. Esto hace el buscador poco útil en la práctica para nombres en español.

## What Changes

- Backend: sustituir el filtro `Name.Contains(q)` (SQL `LIKE` case-sensitive, sin plegado de acentos) por una comparación normalizada en memoria: se pliegan mayúsculas/minúsculas y diacríticos (`á`→`a`, etc.) tanto en el texto buscado como en los nombres candidatos, vía un helper compartido (`TextSearchNormalizer` o similar) usado por `GetPromotersHandler` y `GetOrganicUnitsHandler`.
- Backend: `GetOrganicUnitsHandler` aplica el mismo criterio también al campo `Code`.
- Frontend: añadir a `organic-units-list.component.ts` la misma caja de búsqueda (con debounce) ya existente en `promoters-list.component.ts`, reutilizando el mismo patrón de UI.

**Decisión de diseño:** se descarta la alternativa de extensión PostgreSQL `unaccent` + `ILIKE` a nivel SQL porque el proyecto no tiene infraestructura de tests de integración contra Postgres real (Testcontainers) — los tests unitarios usan EF InMemory, que no traduce funciones específicas de Npgsql, dejando ese camino sin cobertura de tests fiable. Ambos catálogos son tablas de referencia pequeñas (decenas de filas), por lo que traer las filas candidatas a memoria y comparar allí es una compensación razonable: más simple, sin nueva dependencia de infraestructura, y verificable con el mismo patrón de tests ya usado en el proyecto.

Fuera de alcance: no se introduce una abstracción compartida de "catálogo buscable" en el frontend (los dos componentes son casi idénticos pero no se justifica extraer un componente genérico solo para esto); no se toca el catálogo de Tags (no tiene búsqueda ni se ha pedido); no se añade infraestructura de Testcontainers/Postgres real para tests (queda fuera de alcance de esta feature).

## Capabilities

### New Capabilities
- `organic-unit-catalog-search`: búsqueda por nombre/código en el catálogo de Unidades Orgánicas, incluyendo la caja de búsqueda en el listado admin del frontend.

### Modified Capabilities
- `promoter-catalog-search`: la búsqueda por nombre pasa de coincidencia `.Contains()` dependiente de la colación de la BD, a una coincidencia explícitamente insensible a mayúsculas/minúsculas y a acentos/diacríticos.

## Impact

- Nuevo helper compartido en `src/CarteraProyectos.Core/Common/` (normalización de texto para búsqueda)
- `src/CarteraProyectos.Core/Features/Promoters/GetPromoters.cs`
- `src/CarteraProyectos.Core/Features/OrganicUnits/GetOrganicUnits.cs`
- `src/frontend/src/app/features/admin/organic-units/organic-units-list.component.ts`
- Tests unitarios existentes de `GetPromotersHandler` y `GetOrganicUnitsHandler` (casos con acentos/mayúsculas mezcladas)
