## ADDED Requirements

### Requirement: Búsqueda por nombre en el catálogo de Promotores
El sistema SHALL permitir filtrar el listado de promotores (`GET /api/promoters`) por un parámetro de texto opcional `q`, que coincide con promotores cuyo `Name` contiene el texto buscado (coincidencia parcial, mismo operador `.Contains()` que `GetOrganicUnitsQuery`).

#### Scenario: Búsqueda con coincidencias
- **WHEN** un cliente autenticado llama a `GET /api/promoters?q=uni`
- **THEN** el sistema devuelve un `PagedResult<PromoterDto>` que incluye únicamente los promotores cuyo `Name` contiene "uni" (sin distinguir mayúsculas/minúsculas si la colación de la base de datos es case-insensitive, igual que `GetOrganicUnitsQuery`)

#### Scenario: Sin parámetro de búsqueda
- **WHEN** un cliente llama a `GET /api/promoters` sin `q`
- **THEN** el sistema devuelve todos los promotores paginados, igual que antes del cambio (compatibilidad hacia atrás)

#### Scenario: Búsqueda sin coincidencias
- **WHEN** un cliente llama a `GET /api/promoters?q=<texto que no coincide con ningún promotor>`
- **THEN** el sistema devuelve un `PagedResult<PromoterDto>` con `Items` vacío y `Total = 0`

### Requirement: Caja de búsqueda en el listado admin de Promotores
El listado admin de promotores (`promoters-list.component.ts`) SHALL exponer un campo de texto que, al escribir, filtra la tabla llamando a `CatalogsService.getPromoters` con el texto introducido como parámetro `q`, sin recargar la página completa.

#### Scenario: El usuario busca un promotor por nombre
- **WHEN** un Gestor escribe texto en la caja de búsqueda del listado de promotores
- **THEN** la tabla se recarga mostrando solo los promotores cuyo nombre coincide con el texto introducido, reiniciando la paginación a la primera página

#### Scenario: El usuario borra el texto de búsqueda
- **WHEN** un Gestor borra el contenido de la caja de búsqueda
- **THEN** la tabla vuelve a mostrar el listado completo de promotores paginado
