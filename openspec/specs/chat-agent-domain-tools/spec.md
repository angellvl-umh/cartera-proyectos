## Purpose

Amplía `ChatToolCatalog` con tools de lectura y escritura para las áreas del dominio aún no cubiertas por el chat nativo: sprints, épicas, actividad de equipos, backlog, roadmap de cartera, forecast de capacidad, métricas ágiles de proyecto, asignación de equipo a proyecto y catálogos (promotores, unidades orgánicas, tags). Cada tool reutiliza los mismos comandos/queries core y reglas de autorización que su endpoint REST equivalente, con el `personId` resuelto del JWT.

## Requirements

### Requirement: Consulta de sprints, backlog burndown y épicas desde el chat
El sistema SHALL exponer en `ChatToolCatalog` tools de solo lectura para consultar los sprints de un proyecto, el burndown de un sprint concreto, y las épicas de un proyecto, devolviendo los mismos datos que sus endpoints REST equivalentes (`GET /api/projects/{id}/sprints`, `GET /api/projects/{id}/sprints/{sprintId}/burndown`, `GET /api/projects/{id}/epics`).

#### Scenario: Listar sprints de un proyecto
- **WHEN** el modelo invoca `get_sprints` con un `projectId` válido
- **THEN** el sistema devuelve los sprints del proyecto con su estado, fechas y capacidad, igual que el endpoint REST equivalente

#### Scenario: Consultar burndown de un sprint sin fechas definidas
- **WHEN** el modelo invoca `get_sprint_burndown` sobre un sprint sin `StartDate`/`EndDate`
- **THEN** el sistema devuelve el mismo error de negocio que el endpoint REST ("no tiene fechas de inicio y fin definidas"), y el asistente lo comunica al usuario

#### Scenario: Listar épicas de un proyecto
- **WHEN** el modelo invoca `get_epics` con un `projectId` válido
- **THEN** el sistema devuelve las épicas del proyecto con título, prioridad y orden

### Requirement: Creación y transición de sprints desde el chat
El sistema SHALL exponer tools de escritura para crear un sprint y transicionar su estado (activar, completar), reutilizando `CreateSprintCommand` y `TransitionSprintStatusCommand` con el `personId` resuelto del JWT, y aplicando exactamente las mismas reglas de autorización y de máquina de estados que el endpoint REST equivalente.

#### Scenario: Crear un sprint
- **WHEN** el modelo invoca `create_sprint` con nombre y proyecto válidos, tras confirmación del usuario
- **THEN** el sistema crea el sprint en estado Planning y devuelve su id

#### Scenario: Completar un sprint con tareas pendientes sin indicar destino de carry-over
- **WHEN** el modelo invoca `complete_sprint` sobre un sprint con tareas no terminadas y sin especificar `carryOver`
- **THEN** el sistema rechaza la operación con el mismo error que el endpoint REST, exigiendo indicar destino (backlog u otro sprint)

#### Scenario: Persona sin permiso intenta crear un sprint
- **WHEN** el modelo invoca `create_sprint` en nombre de una persona que no es Gestor ni pertenece a un equipo asignado al proyecto
- **THEN** el sistema rechaza la operación con el mismo error de autorización que el endpoint REST

### Requirement: Creación y edición de épicas desde el chat
El sistema SHALL exponer tools de escritura para crear y actualizar épicas de un proyecto, reutilizando `CreateEpicCommand` y `UpdateEpicCommand` sin añadir restricciones de autorización distintas a las que ya aplica el endpoint REST equivalente.

#### Scenario: Crear una épica
- **WHEN** el modelo invoca `create_epic` con proyecto y título válidos, tras confirmación del usuario
- **THEN** el sistema crea la épica y devuelve su id

#### Scenario: Actualizar una épica existente
- **WHEN** el modelo invoca `update_epic` con un id de épica existente, tras confirmación del usuario
- **THEN** el sistema actualiza título, descripción, prioridad y orden de la épica

### Requirement: Consulta de equipos y actividad por equipo desde el chat
El sistema SHALL exponer tools de solo lectura para listar los equipos y para consultar, por cada equipo, en qué tarea activa está cada persona (equivalente a `GET /api/teams/activity`).

#### Scenario: Consultar actividad de equipos
- **WHEN** el modelo invoca `get_team_activity`
- **THEN** el sistema devuelve, por equipo, cada miembro con sus tareas activas actuales (o ninguna si está disponible)

### Requirement: Gestión de backlog desde el chat
El sistema SHALL exponer tools de escritura para reordenar la prioridad de tareas de backlog de un proyecto y para asignar masivamente varias tareas a un sprint (o al backlog), reutilizando `ReorderWorkItemsCommand` y `BulkAssignWorkItemsToSprintCommand` sin añadir restricciones de autorización distintas a las del endpoint REST equivalente.

#### Scenario: Reordenar el backlog
- **WHEN** el modelo invoca `reorder_backlog_item` con una lista ordenada de ids de tareas del proyecto, tras confirmación del usuario
- **THEN** el sistema reasigna el `SortOrder` de esas tareas según el orden recibido

#### Scenario: Asignación masiva a sprint con un id que no pertenece al proyecto
- **WHEN** el modelo invoca `bulk_assign_to_sprint` incluyendo un id de tarea que no pertenece al proyecto indicado
- **THEN** el sistema rechaza la operación completa con el mismo error que el endpoint REST, sin aplicar cambios parciales

### Requirement: Roadmap de cartera y forecast de capacidad desde el chat
El sistema SHALL exponer tools de solo lectura equivalentes a `GET /api/portfolio/roadmap` y `GET /api/capacity/forecast`, con los mismos filtros opcionales (año).

#### Scenario: Consultar el roadmap del año en curso
- **WHEN** el modelo invoca `get_portfolio_roadmap` sin indicar año
- **THEN** el sistema devuelve el roadmap agrupado por equipo primario para el año actual

#### Scenario: Consultar el forecast de capacidad de un año concreto
- **WHEN** el modelo invoca `get_capacity_forecast` con un año explícito
- **THEN** el sistema devuelve la previsión de carga por equipo y trimestre para ese año

### Requirement: Métricas ágiles de proyecto desde el chat
El sistema SHALL exponer tools de solo lectura equivalentes a `GET /api/projects/{id}/velocity` y `GET /api/projects/{id}/cycle-time`.

#### Scenario: Consultar velocity de un proyecto
- **WHEN** el modelo invoca `get_project_velocity` con un `projectId` válido
- **THEN** el sistema devuelve los puntos comprometidos y entregados por sprint completado junto con la media de velocidad

#### Scenario: Consultar cycle time de un proyecto sin tareas completadas
- **WHEN** el modelo invoca `get_project_cycle_time` sobre un proyecto sin tareas en estado Done
- **THEN** el sistema devuelve el mismo resultado vacío/informativo que el endpoint REST equivalente

### Requirement: Asignación de equipo a proyecto desde el chat
El sistema SHALL exponer una tool de escritura equivalente a la asignación de equipo a proyecto (equipo primario o secundario), reutilizando `AssignTeamToProjectCommand`, restringida a Gestor.

#### Scenario: Asignar un equipo secundario a un proyecto
- **WHEN** un Gestor invoca `assign_project_team` con un proyecto y equipo válidos, tras confirmación del usuario
- **THEN** el sistema asigna el equipo al proyecto

#### Scenario: Persona no Gestor intenta asignar un equipo
- **WHEN** el modelo invoca `assign_project_team` en nombre de una persona con rol Desarrollador
- **THEN** el sistema rechaza la operación indicando que solo el Gestor puede asignar equipos a proyectos

### Requirement: Consulta de catálogos desde el chat
El sistema SHALL exponer tools de solo lectura para listar promotores, unidades orgánicas y tags, de forma que el agente pueda resolver el id correspondiente a un nombre antes de crear o actualizar un proyecto.

#### Scenario: Resolver un promotor por nombre antes de crear un proyecto
- **WHEN** el usuario pide crear un proyecto indicando el nombre de un promotor y el modelo invoca `get_promoters` con ese texto como filtro
- **THEN** el sistema devuelve los promotores cuyo nombre coincide, permitiendo al modelo obtener el id antes de llamar a `create_project`
