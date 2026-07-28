## Context

`GetPromotersHandler` y `GetOrganicUnitsHandler` (`src/CarteraProyectos.Core/Features/{Promoters,OrganicUnits}/Get*.cs`) filtran hoy con `Name.Contains(request.Q)` (y `OrganicUnits` también `Code.Contains(request.Q)`) traducido por Npgsql EF Core 10.0.2 a SQL `LIKE`. Ese `LIKE` es case-sensitive (Npgsql no lo traduce a `ILIKE`) y no pliega diacríticos, porque el clúster PostgreSQL de este proyecto (`docker-compose.yml`, imagen `postgres:18` sin `POSTGRES_INITDB_ARGS` ni collation/extension custom) usa la colación por defecto. No existe ninguna extensión Postgres habilitada (`HasPostgresExtension`) ni `HasDbFunction` en el proyecto — se partiría de cero.

Los tests de estas features (`tests/CarteraProyectos.UnitTests/Features/Promoters/PromoterHandlerTests.cs` y equivalente de OrganicUnits) usan `UseInMemoryDatabase`, no Postgres real. No existe ningún proyecto `IntegrationTests` con Testcontainers en el repo pese a que `AGENTS.md` lo menciona como parte del stack objetivo — es aspiracional, no implementado.

## Goals / Non-Goals

**Goals:**
- Buscar "promocion" debe encontrar "Promoción"; buscar "UNI" o "uni" debe encontrar "Unidad" — insensible a mayúsculas/minúsculas y a acentos/diacríticos, en ambos catálogos (Promoters, OrganicUnits) y en ambos campos de OrganicUnits (Name, Code).
- Añadir la caja de búsqueda que falta en `organic-units-list.component.ts`, igual que la de `promoters-list.component.ts`.
- El comportamiento debe ser 100% verificable con el patrón de test existente (xUnit + EF InMemory + Shouldly), sin introducir nueva infraestructura de test.

**Non-Goals:**
- No se monta Testcontainers/Postgres real para tests en este change.
- No se optimiza para catálogos de gran volumen (miles+ de filas) — son catálogos administrables pequeños (Promoters, OrganicUnits), no datos transaccionales.
- No se toca el catálogo de Tags.
- No se extrae un componente Angular genérico de "listado buscable" — se acepta la duplicación entre `promoters-list.component.ts` y `organic-units-list.component.ts` (ya existía antes de este change).

## Decisions

- **Normalización en memoria en vez de `unaccent` + `ILIKE` a nivel SQL.** Alternativa descartada: habilitar la extensión `unaccent` de PostgreSQL vía migración (`HasPostgresExtension`) y mapear `unaccent()` como `HasDbFunction`, combinado con `EF.Functions.ILike`. Se descarta porque (a) EF Core InMemory no traduce funciones específicas de Npgsql ni extensiones SQL, así que esos tests no podrían ejercitar el comportamiento real sin Testcontainers, infraestructura que no existe en el repo; (b) introduce una dependencia de base de datos (extensión) que debe gestionarse en cada entorno (dev/test/prod) vía migración; (c) el volumen de datos de estos catálogos (decenas de filas) hace innecesaria la optimización de filtrar en SQL.
  En su lugar: cuando `Q` no es null/vacío, cargar las filas candidatas del catálogo completo (`ToListAsync()`, sin `Where` de texto en SQL), y filtrar en memoria comparando una versión normalizada (minúsculas + sin diacríticos) del campo contra la versión normalizada de `Q`. La paginación (`Skip`/`Take`) se aplica también en memoria sobre el resultado ya filtrado, en vez de en SQL, cuando hay `Q`. Sin `Q`, el camino existente (orden + `Skip`/`Take` en SQL) no cambia — no hay coste añadido en el caso sin búsqueda, que es el más frecuente (listado paginado sin filtrar).
- **Helper compartido `TextSearchNormalizer`** en `src/CarteraProyectos.Core/Common/TextSearchNormalizer.cs`: un método estático `Normalize(string? text) => string` que hace `text.Normalize(NormalizationForm.FormD)`, elimina los caracteres Unicode de categoría `NonSpacingMark` (los diacríticos separados por la descomposición canónica), y aplica `ToUpperInvariant()`. Usado igual en `GetPromotersHandler` y `GetOrganicUnitsHandler` — evita duplicar la lógica de normalización (que si diverge entre ambos, produciría comportamientos de búsqueda inconsistentes entre catálogos hermanos).
- **Coincidencia parcial (substring), no por palabras**: se mantiene el mismo criterio que el `.Contains()` original — `Normalize(candidato).Contains(Normalize(Q), StringComparison.Ordinal)` — para no cambiar la semántica de "coincidencia parcial" ya documentada en la spec de `promoter-catalog-search`, solo se le añade el plegado de mayúsculas/acentos.
- **OrganicUnits**: el criterio de acierto es "Name coincide O Code coincide" (igual que hoy), cada uno normalizado independientemente.

## Risks / Trade-offs

- [Riesgo] Cargar todas las filas del catálogo a memoria cuando hay `Q` es menos eficiente que filtrar en SQL. → Mitigación: son catálogos administrables (Promoters, OrganicUnits), de tamaño pequeño por diseño (los gestiona un Gestor manualmente); si en el futuro crecen a un tamaño donde esto sea un problema real, se puede revisar (p. ej. columna generada normalizada + índice, o `unaccent` en SQL con Testcontainers ya en su sitio). No se optimiza prematuramente ahora.
- [Riesgo] Divergencia entre el criterio de "normalizado" de este helper y cualquier expectativa de acentuación específica del español (p. ej. la `ñ` no es un diacrítico de `n` en español y `Normalize(FormD)` no la descompone — se mantiene tal cual, correcto). → Mitigación: cubierto con casos de test explícitos (acentos vocálicos, mayúsculas, `ñ` no se pliega a `n`).
- [Riesgo] Cambiar de "filtro en SQL" a "filtro en memoria" cuando hay `Q` podría, en teoría, cambiar el orden de paginación si no se reaplica el mismo `OrderBy(Name)` en memoria. → Mitigación: se ordena en memoria igual que en SQL (`OrderBy(x => x.Name)`) antes de paginar, y se cubre con un test que verifica el orden con `Q` presente.

## Migration Plan

No aplica — no hay migración de base de datos ni cambio de contrato de API (el parámetro `q` ya existe y sigue teniendo la misma forma; solo cambia el criterio interno de coincidencia). Despliegue directo, sin pasos especiales ni rollback distinto al habitual (revertir el commit).
