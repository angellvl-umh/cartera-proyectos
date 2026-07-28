## Context

`Project.SiptGroup` es un enum de 6 valores fijos sin ningún comportamiento asociado (no interviene en máquinas de estado, permisos ni cálculos) — es puro dato descriptivo, hoy sin utilidad según el usuario. Se elimina por completo, sin migración de datos (no hay a dónde migrar el valor: no hay un mapeo SiptGroup→Team).

La asignación de equipos a proyectos YA existe de forma completa en el backend: `ProjectTeamAssignment` (clave compuesta ProjectId+TeamId, `IsPrimary`), `AssignTeamToProjectCommand`/`RemoveTeamFromProjectCommand` con su propia invariante de "solo un equipo primario" (`AssignTeamToProject.cs:38-45`), endpoints `POST/DELETE /api/projects/{id}/teams`, lectura vía `GetProjectQuery.Teams` (`ProjectTeamDto(TeamId, TeamName, IsPrimary)`), y es la base de `ProjectAuthorization.EnsureCanManageProjectAsync`. Lo que falta es exponerlo en el flujo de alta/edición del proyecto (hoy solo se puede asignar equipos después de crear el proyecto, llamada a llamada) y completar el "añadir equipo" que falta en la pestaña de detalle (el servicio Angular ya tiene `assignTeam()`/`getTeams()` sin usar).

## Goals / Non-Goals

**Goals:**
- Quitar `SiptGroup` de dominio, API, agente IA, frontend, seeders y documentación sin dejar referencias rotas.
- Poder asignar uno o más equipos (con un primario opcional) directamente desde el formulario de alta/edición de proyecto.
- Completar la pestaña "Equipos asignados" del detalle con un control para añadir equipos (ya tiene el de quitar).

**Non-Goals:**
- No se añade una tool de agente IA para gestionar equipos de proyecto.
- No se toca el filtro `teamId` (un único equipo) ya existente en listados/informes — es ortogonal a esta feature.
- No se hace backfill ni se conserva el histórico de `SiptGroup` en ningún sitio (tabla de auditoría, columna renombrada, etc.) — se elimina sin más, tal y como ha pedido el usuario.

## Decisions

- **`CreateProjectCommand`/`UpdateProjectCommand` ganan `TeamIds: IReadOnlyList<int>? = null` y `PrimaryTeamId: int? = null`, en el mismo lugar donde estaba `SiptGroup`.** Alternativa descartada: obligar a usar `POST .../teams` tras crear el proyecto desde el formulario (dos llamadas encadenadas desde el frontend). Se descarta porque el usuario pidió poder asignar equipos "al proyecto" como una operación natural del alta/edición — igual que ya ocurre con `TagIds` en el mismo formulario — y porque encadenar llamadas desde el componente Angular complica el manejo de errores parciales (proyecto creado pero fallo al asignar el segundo equipo).
- **Validación de `PrimaryTeamId`:** si se envía, debe estar contenido en `TeamIds` (regla en el validator de FluentValidation: `RuleFor(x => x.PrimaryTeamId).Must((cmd, id) => id is null || (cmd.TeamIds ?? []).Contains(id.Value))`). Si `TeamIds` es null/vacío, `PrimaryTeamId` debe ser null.
- **`CreateProjectHandler`: dos `SaveChangesAsync`.** El proyecto necesita su `Id` real (autogenerado) antes de poder crear filas `ProjectTeamAssignment` (su constructor toma `int projectId`, no una referencia a `Project` con fixup de EF como sí hace `ProjectStatusHistory.Create(project, ...)`). Se guarda primero el proyecto (como hoy), y a continuación, si `TeamIds` tiene elementos, se añaden las `ProjectTeamAssignment` (marcando `IsPrimary` solo en `PrimaryTeamId`) y se guarda de nuevo. Alternativa descartada: cambiar `ProjectTeamAssignment` para aceptar una referencia a `Project` y habilitar el fixup de EF en un único `SaveChanges` — se descarta por ser un cambio de API del constructor de una entidad ampliamente testeada y usada (20+ ficheros de test la construyen directamente), con beneficio marginal (evitar un roundtrip extra en una operación de baja frecuencia como crear un proyecto).
- **`UpdateProjectHandler`: reemplazo completo de `ProjectTeamAssignment` cuando `TeamIds` no es null**, igual que ya hace hoy con `TagIds` (`existingTags.Clear()` + re-add) — se borran las asignaciones que ya no estén en `TeamIds`, se crean las nuevas, y se ajusta `IsPrimary` según `PrimaryTeamId`. Si `TeamIds` es null (no se envía), no se toca la asignación de equipos — permite que otros flujos (`POST/DELETE .../teams` desde el detalle) sigan funcionando de forma independiente sin que un `PUT` de edición los pise accidentalmente.
- **La pestaña de detalle sigue usando `POST/DELETE /api/projects/{id}/teams` directamente** (no pasa por `UpdateProjectCommand`) — es exactamente el flujo que ya existe para "quitar equipo"; añadir "añadir equipo" en el mismo sitio con las mismas llamadas (`ProjectsService.assignTeam`/`getTeams`, ya implementadas) mantiene el patrón simétrico y no requiere tocar el backend para esta parte.
- **`SiptGroup` no se sustituye por ningún otro campo descriptivo.** Se elimina sin más — el propio usuario indicó que "ya no tiene sentido".

## Risks / Trade-offs

- [Riesgo] **BREAKING**: cualquier cliente externo del agente IA (Open WebUI) que hoy envíe `siptGroup` a `create_project`/`update_project` recibirá un error de deserialización (propiedad desconocida se ignora por defecto en System.Text.Json, así que en la práctica no rompe, simplemente el valor se ignora silenciosamente) o dejará de recibirlo en las respuestas de `get_projects`. → Mitigación: se actualiza la descripción OpenAPI de las tools afectadas para que el LLM deje de mencionar `siptGroup` como campo válido; no se requiere versionado de API porque el Tool Server no tiene consumidores externos fuera de esta instancia de Open WebUI.
- [Riesgo] Migración que elimina una columna es irreversible sin backup — se pierde el valor `SiptGroup` de todos los proyectos existentes. → Mitigación: es el comportamiento explícitamente pedido por el usuario (el campo "ya no tiene sentido"); no se requiere backfill.
- [Riesgo] Reemplazo completo de `ProjectTeamAssignment` en `UpdateProjectCommand` podría chocar con ediciones concurrentes (ej. alguien añade un equipo vía detalle mientras otro edita el formulario completo y envía `TeamIds` desactualizado, pisando el cambio). → Mitigación: mismo riesgo que ya asume hoy `TagIds` con el mismo patrón — no es un caso nuevo introducido por este change, es una limitación conocida y aceptada del enfoque "reemplazo completo" ya en uso.

## Migration Plan

1. Migración EF Core: `DropColumn("SiptGroup", "Projects")` (down: recrear columna nullable `character varying`, sin restaurar datos — no es reversible con datos).
2. Desplegar backend + frontend juntos (no hay periodo de compatibilidad necesario: el campo se elimina de raíz, no se deprecia gradualmente).
3. Sin pasos de rollback más allá de revertir el commit y, si la migración ya se aplicó en producción, aceptar la pérdida de la columna (no había datos críticos que preservar).
