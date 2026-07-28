## Why

El catálogo de Promotores (`/api/promoters`) no permite buscar por nombre, a diferencia del catálogo hermano de Unidades Orgánicas (`/api/organic-units`), que ya soporta un parámetro `q` de búsqueda por texto. A medida que el catálogo de promotores crece, localizar uno en la tabla admin obliga a paginar manualmente. Alinear ambos catálogos con el mismo patrón de búsqueda es una mejora pequeña y de bajo riesgo.

## What Changes

- Backend: `GetPromotersQuery`/`GetPromotersHandler` acepta un parámetro `q` opcional; cuando se envía, filtra `Promoter.Name` con `.Contains(q)` (mismo operador y criterio exacto que `GetOrganicUnitsQuery`).
- Backend: el endpoint `GET /api/promoters` expone el nuevo parámetro de query `q` (opcional, sin romper compatibilidad con llamadas existentes).
- Frontend: `CatalogsService.getPromoters` acepta un `q` opcional y lo añade como query param cuando está presente.
- Frontend: `promoters-list.component.ts` incorpora una caja de búsqueda (`nz-input` con icono de búsqueda) que recarga la tabla filtrada por texto al escribir.

Fuera de alcance: no se toca `organic-units-list.component.ts` (tiene el mismo hueco de UI pero queda para un change aparte).

## Capabilities

### New Capabilities
- `promoter-catalog-search`: búsqueda por nombre en el catálogo de Promotores, tanto en el endpoint backend como en el listado admin del frontend.

### Modified Capabilities
(ninguna — no existe spec previa de `promoters` en `openspec/specs/`)

## Impact

- `src/CarteraProyectos.Core/Features/Promoters/GetPromoters.cs` (Query + Handler)
- `src/CarteraProyectos.Api/Endpoints/PromoterEndpoints.cs`
- `src/frontend/src/app/features/admin/catalogs.service.ts`
- `src/frontend/src/app/features/admin/promoters/promoters-list.component.ts`
- Tests unitarios existentes de `GetPromotersHandler` (añadir caso con `q`)
