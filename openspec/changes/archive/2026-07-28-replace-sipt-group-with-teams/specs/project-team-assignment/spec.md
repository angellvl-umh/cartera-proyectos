## ADDED Requirements

### Requirement: Asignar equipos a un proyecto al crearlo
El sistema SHALL permitir especificar, al crear un proyecto (`POST /api/projects`), una lista opcional de ids de equipo (`TeamIds`) y, opcionalmente, cuál de ellos es el equipo primario (`PrimaryTeamId`). Si `PrimaryTeamId` se especifica, SHALL estar incluido en `TeamIds`.

#### Scenario: Crear un proyecto con varios equipos, uno primario
- **WHEN** un Gestor crea un proyecto enviando `TeamIds: [1, 2]` y `PrimaryTeamId: 1`
- **THEN** el proyecto se crea con dos equipos asignados (ids 1 y 2), y el equipo 1 queda marcado como primario

#### Scenario: Crear un proyecto sin equipos
- **WHEN** un Gestor crea un proyecto sin enviar `TeamIds`
- **THEN** el proyecto se crea sin ningún equipo asignado, igual que el comportamiento anterior a este cambio

#### Scenario: PrimaryTeamId no incluido en TeamIds
- **WHEN** un Gestor crea un proyecto enviando `PrimaryTeamId: 3` pero `TeamIds` no incluye el id 3 (o no se envía `TeamIds`)
- **THEN** el sistema rechaza la petición con un error de validación

### Requirement: Reemplazar los equipos asignados a un proyecto al editarlo
El sistema SHALL permitir reemplazar el conjunto completo de equipos asignados a un proyecto existente (`PUT /api/projects/{id}`) enviando `TeamIds` (y opcionalmente `PrimaryTeamId`, sujeto a la misma validación que en la creación). Cuando `TeamIds` no se envía (es `null`), el sistema SHALL dejar intactas las asignaciones de equipo existentes.

#### Scenario: Editar un proyecto para cambiar sus equipos
- **WHEN** un Gestor edita un proyecto que tiene los equipos [1, 2] asignados, enviando `TeamIds: [2, 3]`
- **THEN** el proyecto queda con los equipos 2 y 3 asignados, y el equipo 1 deja de estar asignado

#### Scenario: Editar un proyecto sin tocar sus equipos
- **WHEN** un Gestor edita un proyecto sin incluir `TeamIds` en la petición (p. ej. solo cambia el título)
- **THEN** los equipos previamente asignados al proyecto no se modifican

### Requirement: Añadir un equipo a un proyecto desde su pantalla de detalle
La pantalla de detalle de proyecto (`project-detail.component.ts`, pestaña "Equipos asignados") SHALL exponer un control para asignar un equipo adicional al proyecto (con opción de marcarlo como primario), usando el endpoint `POST /api/projects/{id}/teams` ya existente.

#### Scenario: Añadir un equipo desde el detalle
- **WHEN** un Gestor selecciona un equipo no asignado y confirma "Asignar" en la pestaña "Equipos asignados"
- **THEN** el equipo aparece en la tabla de equipos asignados del proyecto, sin necesidad de recargar la página completa

#### Scenario: Añadir un equipo marcándolo como primario
- **WHEN** un Gestor asigna un equipo desde el detalle marcando la opción "Primario"
- **THEN** ese equipo queda marcado como primario y cualquier otro equipo que fuera primario deja de serlo
