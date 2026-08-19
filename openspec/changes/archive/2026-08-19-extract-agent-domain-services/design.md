## Context

Ver `proposal.md - Why` para el problema. Contexto técnico relevante:

- MediatR está registrado con dos `IPipelineBehavior<,>`: `ValidationBehavior` (ejecuta el `AbstractValidator<TRequest>` de FluentValidation si existe) y `AgentAuditBehavior` (escribe en `AgentActionLog` solo si `TRequest : IAgentAuditable`). Ambos se re-ejecutan en cada `sender.Send()`, incluidos los anidados.
- Solo los `Agent*Command` implementan `IAgentAuditable` — los comandos de dominio internos (`TransitionWorkItemStatusCommand`, etc.) no, así que hoy no hay doble auditoría. Este invariante hay que preservarlo explícitamente porque no hay nada que lo garantice salvo disciplina.
- El `DbContext` es scoped por request HTTP; una cadena de `sender.Send()` anidados comparte la misma instancia, así que no hay problema de concurrencia (las llamadas son secuenciales, no `Task.WhenAll`), pero cada nivel llama a su propio `SaveChangesAsync()`.
- `tests/CarteraProyectos.ArchTests` no existe pese a estar en la tabla de estructura de `AGENTS.md` — hay que crearlo.

## Goals / Non-Goals

**Goals:**
- Ningún `IRequestHandler<>` en `CarteraProyectos.Core` depende de `ISender`/`IMediator`, salvo `SendChatMessageHandler` (única excepción documentada y verificada por test).
- La lógica de negocio que hoy comparten un handler de dominio y su `Agent*Handler` vive en un único sitio (el servicio de aplicación), no duplicada ni re-invocada vía mediator.
- Cero cambios de comportamiento observable: mismos endpoints, mismas reglas de autorización, misma auditoría, mismas respuestas.

**Non-Goals:**
- No se cambia el contrato de ninguna tool del chat (`ChatToolCatalog`) ni el JSON Schema que ve el modelo.
- No se toca el nivel `SendChatMessageHandler → sender.Send(Agent*Command)` — es el despacho dinámico legítimo, ver Decisión 1.
- No se migran las queries de solo lectura que **no** tienen esta cadena (p. ej. `AgentGetMyTasksHandler(IAppDbContext db)` ya accede a BD directamente) — fuera de alcance porque no hay nesting que resolver ahí.
- No se introduce una capa de transacción explícita (Unit of Work con rollback multi-comando) — cada servicio sigue guardando sus propios cambios; si se decide que hace falta atomicidad entre varias tool calls del chat, es un change aparte.

## Decisions

### Decisión 1 — Dónde se permite `ISender`
Regla única: **`ISender` solo se inyecta en Minimal API endpoints y en `SendChatMessageHandler`**. Todo lo demás (handlers de dominio, `Agent*Handler`) depende de servicios de aplicación planos (clases normales, sin `IRequest`).

Alternativa considerada: prohibir `ISender` también en `SendChatMessageHandler` y hacer que el propio bucle de tool-calling llame a los servicios de aplicación directamente, sin pasar por `Agent*Command`/MediatR en absoluto. Se descarta porque el catálogo de tools (`ChatToolCatalog`) está deliberadamente construido para reflejar 1:1 el mismo `IRequest` que ya describe cada acción en `AGENTS.md` ("tools de escritura se envuelven en un `Agent*Command` fino que implementa `IAgentAuditable`") — quitar eso obligaría a reinventar la auditoría y el catálogo de tools fuera de MediatR, un cambio mucho mayor sin beneficio adicional (el problema real era el nesting *entre handlers*, no que `SendChatMessageHandler` despache comandos).

### Decisión 2 — Granularidad de los servicios: uno por área de feature, no uno por comando
Un servicio por agrupación funcional (no uno por cada `Agent*Command`), para no multiplicar clases de una línea. Ubicación: junto a los comandos de dominio de esa feature (`Features/<Feature>/`), siguiendo primary constructors + DI como el resto del proyecto.

| Servicio | Ubicación | Métodos (uno por comando cubierto) | Sustituye el nesting de |
|---|---|---|---|
| `IWorkItemLifecycleService` | `Features/WorkItems/WorkItemLifecycleService.cs` | `TransitionStatusAsync`, `ReorderAsync`, `BulkAssignToSprintAsync` | `TransitionWorkItemStatusHandler`, `ReorderWorkItemsHandler`, `BulkAssignWorkItemsToSprintHandler` + `AgentUpdateTaskStatusHandler`, `AgentReorderBacklogHandler`, `AgentBulkAssignToSprintHandler` |
| `IProjectLifecycleService` | `Features/Projects/ProjectLifecycleService.cs` | `CreateAsync`, `UpdateAsync`, `TransitionStatusAsync`, `AssignTeamAsync` | `CreateProjectHandler`, `UpdateProjectHandler`, `TransitionProjectStatusHandler`, `AssignTeamToProjectHandler` + `AgentCreateProjectHandler`, `AgentUpdateProjectHandler`, `AgentTransitionProjectStatusHandler`, `AgentAssignProjectTeamHandler` |
| `IProjectGovernanceService` | `Features/Projects/ProjectGovernanceService.cs` | `GetRisksAsync`, `AddRiskAsync`, `UpdateRiskAsync`, `GetDependenciesAsync`, `AddDependencyAsync` | `GetProjectRisksHandler`, `AddProjectRiskHandler`, `UpdateProjectRiskHandler`, `GetProjectDependenciesHandler`, `AddProjectDependencyHandler` + sus 5 equivalentes `Agent*` |
| `ISprintLifecycleService` | `Features/Sprints/SprintLifecycleService.cs` | `CreateAsync`, `TransitionStatusAsync` | `CreateSprintHandler`, `TransitionSprintStatusHandler` + `AgentCreateSprintHandler`, `AgentActivateSprintHandler`, `AgentCompleteSprintHandler` |
| `IEpicService` | `Features/Epics/EpicService.cs` | `CreateAsync`, `UpdateAsync` | `CreateEpicHandler`, `UpdateEpicHandler` + `AgentCreateEpicHandler`, `AgentUpdateEpicHandler` |
| `IPersonManagementService` | `Features/Persons/PersonManagementService.cs` | `GetListAsync`, `UpdateAsync`, `SetActiveAsync` | `GetPersonsHandler`, `UpdatePersonHandler`, `SetPersonActiveHandler` + `AgentGetPersonsHandler`, `AgentUpdatePersonHandler`, `AgentSetPersonActiveHandler` |
| `ICapacityReadService` / `IProjectsReadService` / `IMyTasksReadService` | `Features/Agent/*ReadServices.cs` | `GetAsync(...)` | La composición interna entre `AgentGetCapacityHandler`/`AgentGetProjectsHandler`/`AgentGetMyTasksHandler` y los 5 `AgentChart*Handler` + 2 `AgentExport*Handler` que hoy los invocan vía `sender.Send` |

El handler de dominio (invocado por el endpoint REST vía `ISender`) y el `Agent*Handler` correspondiente quedan como **adaptadores finos**: parsean/normalizan su propio `IRequest`, llaman al método del servicio, y listo. La validación de forma (FluentValidation) se queda en cada `IRequest` tal cual está hoy — sigue disparándose una vez por cada entrada pública (endpoint REST o `Agent*Command` despachado desde `SendChatMessageHandler`), simplemente ya no se dispara dos veces en cascada porque el comando de dominio interno deja de pasar por `ISender`.

Alternativa considerada: extraer un servicio por cada comando (1:1). Se descarta por ruido — la mayoría de estos comandos ya son adaptadores de una sola línea (ver `AgentCreateSprintHandler`, `AgentUpdateEpicHandler`), no hay lógica suficiente para justificar una clase por comando.

### Decisión 3 — Invariante de auditoría
Los métodos del servicio no auditan nada. La auditoría se sigue disparando exclusivamente por `AgentAuditBehavior` sobre los `Agent*Command` (que implementan `IAgentAuditable`), en su único `sender.Send()` restante (`SendChatMessageHandler → Agent*Command`). Los comandos de dominio (`TransitionWorkItemStatusCommand`, etc.) nunca deben implementar `IAgentAuditable` — se deja como regla documentada, no verificable automáticamente sin más infraestructura de la que se quiere añadir en este change.

### Decisión 4 — Verificación con ArchTest
Nuevo proyecto `tests/CarteraProyectos.ArchTests` (xUnit + reflexión simple sobre `System.Reflection`, sin librería externa como NetArchTest para no añadir una dependencia nueva solo para una regla): un test que enumera todas las clases del ensamblado `CarteraProyectos.Core` que implementan `IRequestHandler<>`/`IRequestHandler<,>`, comprueba que ninguna tiene un parámetro de constructor de tipo `ISender`/`IMediator`, con una única excepción explícita en el propio test (`SendChatMessageHandler`). Un handler nuevo que rompa la regla hace fallar el build.

## Risks / Trade-offs

- **[Riesgo] Refactor grande y transversal (~20 handlers de dominio + ~20 `Agent*Handler` + 500 tests unitarios existentes que mockean `ISender` en los tests de `Agent*Handler`)** → Mitigación: aplicar por capas/feature (ver `tasks.md`), verificar build+tests tras cada capa antes de pasar a la siguiente, como marca el flujo `/opsx:apply-kiro` del proyecto.
- **[Riesgo] Los tests unitarios existentes de cada `Agent*Handler` seguramente mockean `ISender` con NSubstitute y verifican `sender.Received().Send(...)`** → Mitigación: esos tests pasan a mockear/verificar el servicio de aplicación en su lugar; es un cambio mecánico de setup, no de intención del test.
- **[Riesgo] Al quitar la validación en cascada del comando de dominio interno, si algún `Agent*Command` no tiene su propio `AbstractValidator` para una regla que hoy solo cubría el comando de dominio interno, se pierde esa validación de forma** → Mitigación: revisar caso por caso al mover cada feature (parte de las tasks de cada capa); si falta, añadir el `RuleFor` correspondiente al validator del `Agent*Command`.

## Migration Plan

Sin migración de datos ni de infraestructura — es refactor de código en `CarteraProyectos.Core`. Plan de despliegue: una capa (feature area) por tarea de `tasks.md`, cada una compilable y testeable de forma independiente; se puede mergear incrementalmente sin dejar el árbol roto entre capas porque cada capa toca un conjunto disjunto de ficheros. Rollback: revertir el commit de la capa afectada, ninguna depende de las demás.
