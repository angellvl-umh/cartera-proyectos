## ADDED Requirements

### Requirement: Agregación cronológica de eventos de actividad
El sistema SHALL exponer un feed que agregue, en orden cronológico inverso (más reciente primero), los siguientes tipos de evento: cambio de estado de un proyecto, creación de una tarea, tarea completada, comentario añadido, y actualización semanal de avance registrada.

#### Scenario: Feed con eventos de varios tipos
- **WHEN** existen eventos de distintos tipos en fechas distintas
- **THEN** el sistema los devuelve todos mezclados en una única lista, ordenada por fecha del evento descendente, sin agrupar por tipo

#### Scenario: Cambio de estado de proyecto no incluye la entrada de creación
- **WHEN** se agregan los cambios de estado de un proyecto
- **THEN** la entrada inicial de `ProjectStatusHistory` con `FromStatus` nulo (creada al crear el proyecto) NO aparece en el feed como "cambio de estado"

#### Scenario: Nueva tarea
- **WHEN** se crea una tarea en un proyecto
- **THEN** aparece en el feed un evento de tipo "tarea creada" con la fecha de creación de la tarea

#### Scenario: Tarea completada
- **WHEN** una tarea transiciona a estado `Done`
- **THEN** aparece en el feed un evento de tipo "tarea completada" con la fecha de la transición

### Requirement: Filtrado combinable por proyecto, equipo y persona
El sistema SHALL permitir filtrar el feed por proyecto, por equipo y por persona (autor o actor del evento), de forma independiente o combinada.

#### Scenario: Filtro por proyecto
- **WHEN** se solicita el feed con un `projectId`
- **THEN** solo se devuelven eventos de ese proyecto (incluyendo comentarios de tareas que pertenecen a ese proyecto)

#### Scenario: Filtro por equipo
- **WHEN** se solicita el feed con un `teamId`
- **THEN** solo se devuelven eventos de proyectos que tienen ese equipo asignado

#### Scenario: Filtro por persona
- **WHEN** se solicita el feed con un `personId`
- **THEN** solo se devuelven eventos cuyo autor/actor es esa persona (quien cambió el estado, quien creó/completó la tarea, quien comentó, o quien registró el avance semanal)

#### Scenario: Filtros combinados
- **WHEN** se solicita el feed con `projectId` y `personId` a la vez
- **THEN** solo se devuelven eventos que cumplen ambas condiciones simultáneamente

#### Scenario: Sin filtros
- **WHEN** se solicita el feed sin ningún filtro
- **THEN** se devuelven eventos de todos los proyectos, equipos y personas

### Requirement: Paginación
El sistema SHALL paginar el feed con el mismo contrato de paginación (`page`, `pageSize`, total) ya usado en el resto de listados de la aplicación, con un tope máximo de `pageSize`.

#### Scenario: Página por defecto
- **WHEN** se solicita el feed sin `page` ni `pageSize`
- **THEN** se devuelve la primera página con el tamaño de página por defecto

#### Scenario: pageSize por encima del máximo
- **WHEN** se solicita el feed con un `pageSize` mayor que el máximo permitido
- **THEN** el sistema lo recorta al máximo permitido en lugar de devolver un error

#### Scenario: Total consistente con los filtros aplicados
- **WHEN** se solicita cualquier página del feed con unos filtros dados
- **THEN** el `total` devuelto refleja el número de eventos que cumplen esos filtros, no el total sin filtrar

### Requirement: Visualización del feed en el frontend
El frontend SHALL ofrecer una pantalla de "actividad reciente" que muestre el feed con indicación visual del tipo de cada evento, los mismos filtros (proyecto, equipo, persona) y paginación, accesible desde la navegación de informes.

#### Scenario: Usuario navega al feed de actividad
- **WHEN** un usuario autenticado abre la pantalla de actividad reciente
- **THEN** ve la lista de eventos más recientes con su tipo, proyecto, autor y fecha, y controles para filtrar por proyecto/equipo/persona y para pasar de página
