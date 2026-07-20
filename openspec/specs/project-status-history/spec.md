## Purpose

Histórico auditable de todas las transiciones de estado de un `Project`: quién cambió el estado, cuándo y de qué estado a qué estado. Mismo patrón que ya existe para `Sprint` (`SprintStatusHistory`) y `WorkItem` (`WorkItemStatusHistory`).

## Requirements

### Requirement: Registro de histórico al crear un proyecto
El sistema SHALL registrar una entrada de histórico de estado al crear un `Project`, con `FromStatus` nulo y `ToStatus` igual al estado inicial del proyecto (`Stopped`).

#### Scenario: Creación de proyecto
- **WHEN** un Gestor crea un proyecto mediante `POST /api/projects`
- **THEN** se persiste una entrada de `ProjectStatusHistory` con `FromStatus = null`, `ToStatus = Stopped`, `ChangedById` igual al Id de quien lo creó y `ChangedAt` igual al instante de creación

### Requirement: Registro de histórico en cada transición de estado
El sistema SHALL registrar una entrada de histórico cada vez que el estado de un `Project` cambia mediante una transición válida.

#### Scenario: Transición válida de estado
- **WHEN** un Gestor o un miembro de un equipo asignado al proyecto transiciona el estado de un proyecto (p. ej. `PlanningWithClient → WaitingForDevelopers`) mediante `PUT /api/projects/{id}/status`
- **THEN** se persiste una entrada de `ProjectStatusHistory` con `FromStatus` igual al estado anterior, `ToStatus` igual al nuevo estado, `ChangedById` igual al Id de quien hizo la transición y `ChangedAt` igual al instante del cambio

#### Scenario: Transición inválida rechazada
- **WHEN** se intenta una transición no permitida por la máquina de estados de `Project` (p. ej. `Completed → InSprint`, siendo `Completed` terminal)
- **THEN** la transición se rechaza con el mismo error que hoy lanza `Project.TransitionTo`, y NO se crea ninguna entrada de histórico

### Requirement: Consulta del histórico de un proyecto
El sistema SHALL exponer el histórico completo de transiciones de un proyecto, ordenado cronológicamente ascendente, incluyendo el nombre de quién hizo cada cambio.

#### Scenario: Consulta con histórico existente
- **WHEN** cualquier usuario autenticado solicita `GET /api/projects/{id}/status-history` de un proyecto con transiciones registradas
- **THEN** el sistema devuelve la lista completa de entradas ordenadas por `ChangedAt` ascendente, cada una con `FromStatus` (o null para la primera), `ToStatus`, `ChangedById`, `ChangedByName` y `ChangedAt`

#### Scenario: Proyecto inexistente
- **WHEN** se solicita `GET /api/projects/{id}/status-history` para un `id` de proyecto que no existe
- **THEN** el sistema devuelve `404 Not Found`

### Requirement: Visualización del histórico en el detalle del proyecto
El frontend SHALL permitir consultar el histórico de estados de un proyecto desde su página de detalle, con la misma experiencia ya usada para el histórico de sprints y tareas.

#### Scenario: Usuario abre el histórico desde el detalle del proyecto
- **WHEN** un usuario con acceso al detalle de un proyecto pulsa el control de histórico junto al estado actual del proyecto
- **THEN** se abre un modal con una tabla (De / A / Quién / Cuándo) con todas las transiciones del proyecto, ordenadas cronológicamente
