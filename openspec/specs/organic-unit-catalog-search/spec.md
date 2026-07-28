# organic-unit-catalog-search

## Purpose

Búsqueda por nombre o código en el catálogo administrable de Unidades Orgánicas, insensible a mayúsculas/minúsculas y a acentos/diacríticos.

## Requirements

### Requirement: Búsqueda por nombre o código en el catálogo de Unidades Orgánicas
El sistema SHALL permitir filtrar el listado de unidades orgánicas (`GET /api/organic-units`) por un parámetro de texto opcional `q`, que coincide con unidades orgánicas cuyo `Name` o `Code` contiene el texto buscado (coincidencia parcial), de forma insensible a mayúsculas/minúsculas y a acentos/diacríticos.

#### Scenario: Búsqueda con coincidencias por nombre
- **WHEN** un cliente autenticado llama a `GET /api/organic-units?q=unidad`
- **THEN** el sistema devuelve un `PagedResult<OrganicUnitDto>` que incluye únicamente las unidades orgánicas cuyo `Name` contiene "unidad"

#### Scenario: Búsqueda con coincidencias por código
- **WHEN** un cliente autenticado llama a `GET /api/organic-units?q=TIC`
- **THEN** el sistema devuelve un `PagedResult<OrganicUnitDto>` que incluye las unidades orgánicas cuyo `Code` contiene "TIC", además de las que coincidan por `Name`

#### Scenario: Búsqueda insensible a mayúsculas y acentos
- **WHEN** un cliente llama a `GET /api/organic-units?q=promocion` y existe una unidad orgánica con `Name` = "Dirección de Promoción"
- **THEN** el sistema incluye esa unidad orgánica en el resultado, sin importar que la búsqueda no incluya tilde ni coincida en mayúsculas/minúsculas

#### Scenario: Sin parámetro de búsqueda
- **WHEN** un cliente llama a `GET /api/organic-units` sin `q`
- **THEN** el sistema devuelve todas las unidades orgánicas paginadas, igual que antes del cambio (compatibilidad hacia atrás)

#### Scenario: Búsqueda sin coincidencias
- **WHEN** un cliente llama a `GET /api/organic-units?q=<texto que no coincide con ninguna unidad orgánica>`
- **THEN** el sistema devuelve un `PagedResult<OrganicUnitDto>` con `Items` vacío y `Total = 0`

### Requirement: Caja de búsqueda en el listado admin de Unidades Orgánicas
El listado admin de unidades orgánicas (`organic-units-list.component.ts`) SHALL exponer un campo de texto que, al escribir, filtra la tabla llamando a `CatalogsService.getOrganicUnits` con el texto introducido como parámetro `q`, sin recargar la página completa.

#### Scenario: El usuario busca una unidad orgánica por nombre o código
- **WHEN** un Gestor escribe texto en la caja de búsqueda del listado de unidades orgánicas
- **THEN** la tabla se recarga mostrando solo las unidades orgánicas cuyo nombre o código coincide con el texto introducido, reiniciando la paginación a la primera página

#### Scenario: El usuario borra el texto de búsqueda
- **WHEN** un Gestor borra el contenido de la caja de búsqueda
- **THEN** la tabla vuelve a mostrar el listado completo de unidades orgánicas paginado
