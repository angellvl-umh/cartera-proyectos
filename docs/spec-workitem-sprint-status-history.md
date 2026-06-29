# Spec: Cambio de estado desde edición + histórico de estados (WorkItem y Sprint)

## Contexto

Hoy el Status de un `WorkItem` solo se puede cambiar arrastrando en el Kanban
(`TransitionWorkItemStatusCommand`). El formulario de edición de tarea
(`project-detail.component.ts`) no expone ese campo. Tampoco existe ningún
registro histórico de los cambios de estado de `WorkItem` ni de `Sprint`: solo
se conserva el valor actual en la columna `Status`. `AgentActionLog` es el
único precedente de tabla de auditoría, pero es específico del agente IA
(payload JSON libre) y no se reutiliza aquí.

Esta spec cubre tres piezas relacionadas que se implementan juntas porque
comparten el mismo patrón (registrar histórico al transicionar estado):

1. Selector de Status en el formulario de edición/creación de `WorkItem`.
2. Histórico de estados de `WorkItem` (`WorkItemStatusHistory`).
3. Histórico de estados de `Sprint` (`SprintStatusHistory`).

---

## 1. Selector de Status en el formulario de WorkItem

### Criterios de aceptación

1. **Given** un Gestor o JefeEquipo de un equipo asignado al proyecto edita una tarea,
   **when** abre el modal de edición, **then** ve un selector de Status con las opciones
   `Backlog, ToDo, InProgress, Blocked, Done` (deshabilitado si el estado actual es `Done`,
   ya que es terminal).
2. **Given** un Desarrollador abre el modal de edición de una tarea que tiene asignada,
   **when** ve el formulario, **then** el selector de Status está habilitado.
3. **Given** un Desarrollador abre el modal de edición de una tarea que NO tiene asignada,
   **when** ve el formulario, **then** el selector de Status está deshabilitado (oculto u
   oculto+readonly, a elección de `/frontend-dev`; mínimo: disabled con tooltip "Solo el
   asignado puede cambiar el estado").
4. **Given** el usuario cambia el Status en el formulario y pulsa "Aceptar",
   **when** el Status difiere del valor original, **then** el frontend llama primero a
   `PUT /api/projects/{projectId}/workitems/{id}` (resto de campos) y después a
   `POST /api/projects/{projectId}/workitems/{id}/status` (ya existente) con el nuevo
   Status — en ese orden, para no perder cambios de otros campos si la transición de
   estado falla por regla de negocio.
5. **Given** la transición de estado es inválida (p. ej. tarea ya `Done`, o el usuario no
   tiene permiso), **when** se llama al endpoint de status, **then** se muestra el error
   del backend (400/403) sin descartar los demás cambios ya guardados por el PUT.
6. **No** se modifica `UpdateWorkItemCommand` ni `UpdateWorkItem.cs`: ese comando sigue sin
   tocar `Status`. El cambio de estado siempre pasa por
   `TransitionWorkItemStatusCommand`, igual que el Kanban — así toda la lógica de
   autorización y el registro de histórico (punto 2) quedan en un único sitio.

### Componentes UI

- `project-detail.component.ts`: añadir `<nz-select>` de Status al formulario de
  WorkItem (sección donde están Prioridad/Tipo), junto a un campo de solo lectura "Sin
  cambios" cuando el status no varía.
- Deshabilitar la opción `Done` para personas que no sean Gestor/JefeEquipo si la tarea
  no es suya — usar el mismo criterio que ya aplica el backend (no se valida en frontend
  de forma autoritativa, solo UX; el backend es la fuente de verdad).
- Al guardar: si `workItemForm.status !== original.status`, encadenar la llamada al
  endpoint de transición tras el `update()` exitoso.

### Permisos (igual que tabla de CLAUDE.md "Cambiar estado tarea")

| Rol | Selector visible | Selector habilitado |
|---|---|---|
| Gestor | Sí | Sí |
| JefeEquipo | Sí | Sí, si lidera un equipo asignado al proyecto |
| Desarrollador | Sí | Sí, solo si la tarea le está asignada |

---

## 2. Histórico de estados de WorkItem

### Modelo de datos

Nueva entidad `WorkItemStatusHistory` (`CarteraProyectos.Core/Domain/WorkItemStatusHistory.cs`):

```csharp
public class WorkItemStatusHistory
{
    public int Id { get; private set; }
    public int WorkItemId { get; private set; }
    public WorkItemStatus? FromStatus { get; private set; }   // null en el registro de creación
    public WorkItemStatus ToStatus { get; private set; }
    public int ChangedById { get; private set; }
    public DateTime ChangedAt { get; private set; }           // UTC

    public WorkItem? WorkItem { get; private set; }
    public Person? ChangedBy { get; private set; }

    public static WorkItemStatusHistory Create(int workItemId, WorkItemStatus? fromStatus,
        WorkItemStatus toStatus, int changedById)
        => new() { WorkItemId = workItemId, FromStatus = fromStatus, ToStatus = toStatus,
                   ChangedById = changedById, ChangedAt = DateTime.UtcNow };
}
```

`AppDbContext`: añadir `DbSet<WorkItemStatusHistory> WorkItemStatusHistories`, configurar
en `OnModelCreating`:
```csharp
modelBuilder.Entity<WorkItemStatusHistory>(e =>
{
    e.HasKey(h => h.Id);
    e.Property(h => h.FromStatus).HasConversion<string>();
    e.Property(h => h.ToStatus).HasConversion<string>().IsRequired();
    e.HasOne(h => h.WorkItem).WithMany().HasForeignKey(h => h.WorkItemId).OnDelete(DeleteBehavior.Cascade);
    e.HasOne(h => h.ChangedBy).WithMany().HasForeignKey(h => h.ChangedById).OnDelete(DeleteBehavior.Restrict);
    e.HasIndex(h => h.WorkItemId);
});
```

Nueva migración EF: `AddWorkItemStatusHistory`.

### Dónde se registra

- **`TransitionWorkItemStatusHandler`** (`Core/Features/WorkItems/TransitionWorkItemStatus.cs`):
  tras `workItem.TransitionStatus(request.NewStatus)` y antes de `SaveChangesAsync`,
  añadir `db.WorkItemStatusHistories.Add(WorkItemStatusHistory.Create(workItem.Id,
  oldStatus, request.NewStatus, request.RequestingPersonId))`. **Requiere capturar
  `oldStatus = workItem.Status` antes de transicionar.**
  - Si `RequestingPersonId == 0` (caller sin contexto de usuario, p. ej. agente IA),
    sigue registrando histórico pero con `ChangedById` apuntando a la persona resuelta
    por el endpoint del agente (ver `CurrentUser.ResolveAsync`, que ya se usa en
    `/status`). El comando NO debe aceptar `ChangedById == 0` como válido para el
    histórico: si no hay persona resuelta, el endpoint ya devuelve 401 antes de llamar
    al mediator (ver `WorkItemEndpoints.cs:78-79`), así que en la práctica
    `RequestingPersonId` siempre será > 0 cuando se llega al handler vía HTTP.
- **`CreateWorkItemHandler`** (`Core/Features/WorkItems/CreateWorkItem.cs`): al crear la
  tarea (Status inicial = `Backlog`), registrar un primer historial con `FromStatus =
  null, ToStatus = Backlog, ChangedById = <creador>`. Esto requiere que
  `CreateWorkItemCommand` reciba el `RequestingPersonId` del creador (hoy no lo recibe);
  añadir el parámetro siguiendo el mismo patrón que `TransitionWorkItemStatusCommand`, y
  que `WorkItemEndpoints.MapPost("/")` resuelva el `CurrentUser` igual que ya hace el
  endpoint de `/status`.

### Endpoint de consulta

```
GET /api/projects/{projectId}/workitems/{id}/status-history
```

- Response `200 OK`: `IReadOnlyList<WorkItemStatusHistoryDto>` ordenado por `ChangedAt`
  ascendente.
  ```csharp
  record WorkItemStatusHistoryDto(int Id, string? FromStatus, string ToStatus,
      int ChangedById, string ChangedByName, DateTime ChangedAt);
  ```
- `404 Not Found` si el `WorkItem` no existe o no pertenece a `projectId`.
- No paginado (un WorkItem no acumula un volumen que lo requiera; si en el futuro crece,
  aplicar el estándar de paginación del proyecto).
- Nuevo `GetWorkItemStatusHistoryQuery(int WorkItemId) : IRequest<IReadOnlyList<WorkItemStatusHistoryDto>>`
  en `Core/Features/WorkItems/`, sin filtros de autorización adicionales (mismo acceso de
  lectura que el resto de detalle de WorkItem — cualquier rol autenticado puede ver el
  Kanban completo según la matriz de permisos).

### UI

- En el modal de edición de WorkItem (`project-detail.component.ts`), añadir una sección
  colapsable "Histórico de estados" (o un botón "Ver histórico" que abra un `nz-modal`
  secundario) que liste: `FromStatus → ToStatus`, `ChangedByName`, `ChangedAt` (formato
  `dd/MM/yyyy HH:mm`). Cargar vía `workItemsService.getStatusHistory(projectId, id)` al
  abrir el modal de edición (solo si `id` existe, es decir, no en modo creación).

---

## 3. Histórico de estados de Sprint

Mismo patrón que WorkItem, aplicado a `Sprint`.

### Modelo de datos

`SprintStatusHistory` (`CarteraProyectos.Core/Domain/SprintStatusHistory.cs`):

```csharp
public class SprintStatusHistory
{
    public int Id { get; private set; }
    public int SprintId { get; private set; }
    public SprintStatus? FromStatus { get; private set; }   // null en el registro de creación (Planning)
    public SprintStatus ToStatus { get; private set; }
    public int ChangedById { get; private set; }
    public DateTime ChangedAt { get; private set; }

    public Sprint? Sprint { get; private set; }
    public Person? ChangedBy { get; private set; }

    public static SprintStatusHistory Create(int sprintId, SprintStatus? fromStatus,
        SprintStatus toStatus, int changedById)
        => new() { SprintId = sprintId, FromStatus = fromStatus, ToStatus = toStatus,
                   ChangedById = changedById, ChangedAt = DateTime.UtcNow };
}
```

`AppDbContext`: `DbSet<SprintStatusHistory> SprintStatusHistories`, configuración análoga
a `WorkItemStatusHistory` (FK a `Sprint` con `Cascade`, FK a `Person` con `Restrict`,
índice por `SprintId`).

Misma migración EF que el punto 2, o una separada: `AddSprintStatusHistory` (recomendado
separarla de `AddWorkItemStatusHistory` para mantener migraciones atómicas por entidad).

### Cambio requerido en `TransitionSprintStatusCommand`

Hoy `TransitionSprintStatusCommand(int Id, SprintStatus NewStatus)` **no** recibe quién
hace el cambio. Hay que añadir `RequestingPersonId`:

```csharp
public record TransitionSprintStatusCommand(int Id, SprintStatus NewStatus, int RequestingPersonId) : IRequest;
```

Y en `SprintEndpoints.cs`, el endpoint `POST /{id:int}/status` debe resolver el usuario
actual con `CurrentUser.ResolveAsync` (mismo patrón que `WorkItemEndpoints.cs:78-79`) y
devolver `401` si no se resuelve, antes de enviar el comando.

En `TransitionSprintStatusHandler`, capturar `oldStatus = sprint.Status` antes de
`sprint.TransitionStatus(...)` y añadir el registro de histórico antes de
`SaveChangesAsync`.

### Registro en creación de Sprint

`CreateSprintHandler` (`Core/Features/Sprints/CreateSprint.cs`): igual que WorkItem,
registrar `FromStatus = null, ToStatus = Planning` al crear. Requiere añadir
`RequestingPersonId` a `CreateSprintCommand` y resolver el usuario en el endpoint
`POST /api/projects/{projectId}/sprints`.

### Endpoint de consulta

```
GET /api/projects/{projectId}/sprints/{id}/status-history
```

- Response `200 OK`: `IReadOnlyList<SprintStatusHistoryDto>` (mismo shape que WorkItem,
  con `FromStatus`/`ToStatus` como string).
- `404 Not Found` si el Sprint no existe o no pertenece a `projectId`.
- `GetSprintStatusHistoryQuery(int SprintId) : IRequest<IReadOnlyList<SprintStatusHistoryDto>>`
  en `Core/Features/Sprints/`.

### UI

En la tabla de Sprints del tab "Sprints" de `project-detail.component.ts`, añadir una
acción "Histórico" (icono `clock-circle` o `history`, ya disponible `ClockCircleOutline`
en `app.config.ts`) por fila que abra un `nz-modal` listando
`FromStatus → ToStatus | ChangedByName | ChangedAt`.

---

## Permisos (lectura de histórico)

El histórico es de solo lectura y sigue la misma visibilidad que el recurso al que
pertenece (Kanban/Sprints visibles para todos los roles autenticados, según matriz de
CLAUDE.md). No se introduce ninguna restricción nueva: cualquier persona autenticada que
puede ver el WorkItem/Sprint puede ver su histórico.

## Casos edge y validaciones

1. Intentar transicionar una tarea `Done` → cualquier otro estado: ya falla en
   `WorkItem.TransitionStatus` con `InvalidOperationException` (400). No se registra
   histórico porque la excepción se lanza antes de llegar al `SaveChangesAsync` — correcto,
   no debe quedar un registro de un cambio que no ocurrió.
2. Intentar transicionar un Sprint fuera de las transiciones permitidas (p. ej.
   `Completed → Active`): ya falla en `Sprint.TransitionStatus`. Mismo razonamiento: sin
   registro si la transición no se aplica.
3. Borrado de un `WorkItem` o `Sprint`: el histórico se borra en cascada
   (`DeleteBehavior.Cascade` sobre la FK `WorkItemId`/`SprintId`). Es una decisión
   deliberada: no se mantiene auditoría de entidades eliminadas en esta iteración (fuera
   de alcance; si se necesitara retención post-borrado, sería una spec aparte con
   borrado lógico).
4. `ChangedById` de una persona que luego se elimina: `DeleteBehavior.Restrict` en la FK
   a `Person` — no se podrá borrar una `Person` que tenga registros de histórico. Esto es
   coherente con cómo ya se trata `Comment.AuthorId` en el modelo actual.
5. Crear un WorkItem/Sprint vía el agente IA (`/api/agent/*`): debe seguir generando el
   registro de creación en el histórico con el `ChangedById` de la persona resuelta por
   `X-Open-WebUI-User-Email` (regla de negocio 5 de CLAUDE.md) — no requiere cambios
   adicionales si el agente reutiliza los mismos `CreateWorkItemCommand`/`CreateSprintCommand`.

## Tests unitarios requeridos (mínimo)

- `TransitionWorkItemStatusHandler`: registra histórico con `FromStatus`/`ToStatus`
  correctos; no registra nada si la transición lanza excepción.
- `CreateWorkItemHandler`: registra histórico inicial `FromStatus = null, ToStatus = Backlog`.
- `TransitionSprintStatusHandler`: idem WorkItem.
- `CreateSprintHandler`: registra histórico inicial `FromStatus = null, ToStatus = Planning`.
- `GetWorkItemStatusHistoryQuery` / `GetSprintStatusHistoryQuery`: devuelve lista
  ordenada por fecha; `KeyNotFoundException` si el recurso no existe.

---

## Resumen de archivos afectados

**Backend:**
- `Core/Domain/WorkItemStatusHistory.cs` (nuevo)
- `Core/Domain/SprintStatusHistory.cs` (nuevo)
- `Core/Interfaces/IAppDbContext.cs` (añadir los dos `DbSet`)
- `Infrastructure/Persistence/AppDbContext.cs` (DbSets + OnModelCreating)
- `Infrastructure/Persistence/Migrations/` (2 migraciones nuevas)
- `Core/Features/WorkItems/TransitionWorkItemStatus.cs` (registrar histórico)
- `Core/Features/WorkItems/CreateWorkItem.cs` (+ RequestingPersonId, registrar histórico)
- `Core/Features/WorkItems/GetWorkItemStatusHistory.cs` (nuevo)
- `Core/Features/Sprints/TransitionSprintStatus.cs` (+ RequestingPersonId, registrar histórico)
- `Core/Features/Sprints/CreateSprint.cs` (+ RequestingPersonId, registrar histórico)
- `Core/Features/Sprints/GetSprintStatusHistory.cs` (nuevo)
- `Api/Endpoints/WorkItemEndpoints.cs` (+ endpoint status-history, resolver CurrentUser en POST /)
- `Api/Endpoints/SprintEndpoints.cs` (+ endpoint status-history, resolver CurrentUser en POST / y POST /status)
- Tests en `tests/CarteraProyectos.UnitTests/Features/WorkItems/` y `.../Sprints/`

**Frontend:**
- `features/projects/workitems.service.ts` (+ `getStatusHistory`, método ya existente
  `transitionStatus` se reutiliza)
- `features/projects/sprints.service.ts` o equivalente (+ `getStatusHistory`)
- `features/projects/project-detail/project-detail.component.ts`:
  - Selector de Status en el form de WorkItem
  - Sección/modal de histórico de WorkItem
  - Acción "Histórico" + modal en la tabla de Sprints

---

## Modelo recomendado para implementación (kiro-cli)

| Tarea | Modelo | Razón |
|---|---|---|
| Entidades + migraciones + DbContext | `claude-haiku-4.5` | CRUD mecánico siguiendo patrón existente |
| Handlers (Transition/Create + histórico) | `claude-sonnet-4.6` | Toca lógica de negocio existente y hay que coordinarla con tests sin romper comportamiento actual |
| Endpoints nuevos de consulta | `claude-haiku-4.5` | Sigue el patrón de endpoints ya existente al pie de la letra |
| Tests unitarios | `claude-haiku-4.5` | Sobre código ya implementado |
| Frontend (selector de Status + modales de histórico) | `claude-sonnet-4.6` | Múltiples archivos (component + service) con contrato TS compartido |

---

¿Apruebas esta spec para pasar a `/backend-dev`? Si quieres ajustar algo (p. ej. dónde se
muestra el histórico en la UI, o si el histórico debe ser visible para todos los roles),
dímelo antes de continuar.
