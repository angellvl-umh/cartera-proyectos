## 1. Backend

- [x] 1.1 Crear `TextSearchNormalizer` en `src/CarteraProyectos.Core/Common/TextSearchNormalizer.cs`: método estático `Normalize(string? text)` que descompone Unicode (`NormalizationForm.FormD`), elimina los caracteres de categoría `NonSpacingMark` y aplica `ToUpperInvariant()`
- [x] 1.2 En `GetPromotersHandler` (`src/CarteraProyectos.Core/Features/Promoters/GetPromoters.cs`): cuando `Q` no sea null/vacío, cargar los promotores (`ToListAsync()`), filtrar en memoria comparando `TextSearchNormalizer.Normalize(p.Name)` contra `TextSearchNormalizer.Normalize(request.Q)` con `.Contains(..., StringComparison.Ordinal)`, ordenar por `Name` y paginar (`Skip`/`Take`) en memoria; sin `Q`, mantener el camino actual (orden + paginación en SQL)
- [x] 1.3 En `GetOrganicUnitsHandler` (`src/CarteraProyectos.Core/Features/OrganicUnits/GetOrganicUnits.cs`): mismo patrón que 1.2, aplicando el criterio a `Name` O `Code` (cada uno normalizado independientemente)

## 2. Frontend (usar opencode, no kiro-cli)

- [x] 2.1 Añadir a `organic-units-list.component.ts` (`src/frontend/src/app/features/admin/organic-units/organic-units-list.component.ts`) la misma caja de búsqueda (`nz-input` con icono, debounce 300ms, `distinctUntilChanged`) que ya existe en `promoters-list.component.ts`, incluyendo el `Subject` de búsqueda, el signal `searchText`, y el reinicio de página a 1 al buscar
- [x] 2.2 Actualizar `load()`/`loadPage()` de `organic-units-list.component.ts` para pasar el texto de búsqueda a `CatalogsService.getOrganicUnits(q, page, pageSize)` (el servicio ya soporta `q`, solo falta conectarlo desde el componente)

## 3. Tests

- [x] 3.1 Test unitario de `TextSearchNormalizer`: acentos vocálicos (`á/é/í/ó/ú` → `A/E/I/O/U`), mayúsculas/minúsculas mezcladas. Nota: se descubrió durante la implementación que `NormalizationForm.FormD` de .NET SÍ descompone `ñ` (U+00F1) en `n` + tilde combinante (U+0303), que se elimina como `NonSpacingMark` — es decir, `ñ` SÍ se pliega a `N` (comportamiento distinto al asumido originalmente en este task, corregido y documentado en el propio helper y sus tests)
- [x] 3.2 Test unitario `GetPromotersHandler`: búsqueda con acentos/mayúsculas distintas encuentra coincidencias (ej. buscar "promocion" encuentra "Promoción"); test existente `GetPromoters_WithSearchQuery_FiltersPartialMatch` actualizado para reflejar el nuevo comportamiento insensible a mayúsculas
- [x] 3.3 Test unitario `GetOrganicUnitsHandler`: mismo caso con acentos/mayúsculas, tanto para `Name` como para `Code`
- [x] 3.4 Test unitario: con `Q` presente, el orden de resultados sigue siendo por `Name` y la paginación (`Page`/`PageSize`) sigue funcionando correctamente
