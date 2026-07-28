## Context

`GetOrganicUnitsQuery` ya implementa búsqueda por nombre vía un parámetro `q` opcional, filtrando con `.Where(u => u.Name.Contains(request.Q) || ...)` (ver `src/CarteraProyectos.Core/Features/OrganicUnits/GetOrganicUnits.cs`). `GetPromotersQuery` es el mismo tipo de catálogo administrable (mismo CRUD, mismo endpoint group pattern) pero no tiene ese parámetro. El cambio es replicar el patrón exacto, no diseñar uno nuevo.

## Goals / Non-Goals

**Goals:**
- Mismo comportamiento de búsqueda que `GetOrganicUnitsQuery`: `q` opcional vía `.Contains()`, sin romper llamadas existentes sin `q`.
- Frontend con una caja de búsqueda funcional en `promoters-list.component.ts`.

**Non-Goals:**
- No se añade búsqueda a `organic-units-list.component.ts` (gap de UI preexistente, fuera de alcance).
- No se cambia la paginación ni el contrato de `PagedResult<PromoterDto>`.

## Decisions

- **Reutilizar el filtro de `GetOrganicUnitsQuery` tal cual** (`.Contains()`) en lugar de introducir una abstracción compartida de "catálogo buscable". Alternativa descartada: extraer un helper genérico — no se justifica para dos catálogos y añadiría indirección sin beneficio claro.
- **Debounce en el frontend antes de recargar la tabla** al escribir en el buscador, para no disparar una request por cada tecla (mismo criterio ya usado en otros buscadores del frontend).

## Risks / Trade-offs

- [Riesgo] Cambiar la firma de `CatalogsService.getPromoters` podría romper otros llamadores → Mitigación: `q` se añade como parámetro opcional al final, compatible con las llamadas existentes.
- [Riesgo] Ninguno relevante en backend: el parámetro es opcional y el filtro solo se aplica si `q` no es null/vacío.
