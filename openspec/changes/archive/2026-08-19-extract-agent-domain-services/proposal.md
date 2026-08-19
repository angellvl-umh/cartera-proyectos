## Why

El catálogo de tools del agente IA (`Features/Agent/*`, invocado hoy desde `Features/Chat/SendChatMessageHandler`) reutiliza la lógica de dominio llamando a los mismos `IRequest` que usan los endpoints REST, pero lo hace **inyectando `ISender` dentro de otro `IRequestHandler`** (`Agent*Handler.Handle()` → `sender.Send(DomainCommand)`). Con el chat nativo esto se convirtió en una cadena de hasta 3 niveles de handlers invocándose entre sí vía MediatR (`SendChatMessageHandler` → `Agent*Handler` → handler de dominio), lo cual:

- reejecuta el pipeline de MediatR (`ValidationBehavior`, `AgentAuditBehavior`) en cada nivel sin que haya un límite ni un test que lo vigile;
- diluye el unit-of-work: cada handler anidado hace su propio `SaveChangesAsync` sobre el mismo `DbContext` scoped, sin una decisión explícita sobre atomicidad;
- dificulta testear un handler en aislamiento (hace falta resolver el árbol completo de handlers vía `ISender`);
- no está prohibido en ningún sitio — nada impide que la cadena crezca a un 4º o 5º nivel.

No es una regresión de comportamiento observable (mismos endpoints, misma autorización, misma auditoría), es deuda de arquitectura que conviene cortar ahora que el catálogo de tools ya cubre ~45 acciones y seguirá creciendo.

## What Changes

- Extraer a un **servicio de aplicación** (clase plana, sin `IRequest`/`ISender`) la lógica que hoy vive dentro de cada handler de dominio que también es invocado por un `Agent*Handler`. El handler de dominio (usado por MediatR desde los endpoints REST) y el `Agent*Handler` correspondiente pasan a inyectar y llamar directamente a ese servicio — ninguno de los dos vuelve a depender de `ISender`.
- Aplicar lo mismo a los handlers de lectura que hoy se llaman entre sí dentro de `Features/Agent` (los `AgentChart*Handler` y `AgentExport*Handler` llaman a `sender.Send(AgentGetCapacityQuery)`, `sender.Send(AgentGetProjectsQuery)`, etc. en vez de compartir un servicio de lectura).
- `SendChatMessageHandler` mantiene su dependencia de `ISender` — es el único punto legítimo de despacho dinámico (el modelo decide en tiempo de ejecución qué tool/`Agent*Command` invocar) — pero pasa a ser la **única** excepción documentada y verificada por test.
- Añadir un test de arquitectura (nuevo proyecto `tests/CarteraProyectos.ArchTests`, ya mencionado en `AGENTS.md` pero inexistente) que falle si cualquier `IRequestHandler<>` del ensamblado `Core` distinto de `SendChatMessageHandler` depende de `ISender`/`IMediator`.
- Documentar la convención ("los handlers no llaman a otros handlers vía `ISender`; la lógica compartida vive en servicios de aplicación") en `.ai/skills/dotnet10/SKILL.md`.
- **BREAKING (interno, no de API)**: cambia la firma del constructor de ~20 clases `Agent*Handler` y de los handlers de dominio equivalentes (pasan a recibir el servicio de aplicación en vez de, o además de, `ISender`). No cambia ningún contrato HTTP ni el comportamiento observable.

## Capabilities

Refactor puro de arquitectura interna: no cambia el comportamiento de ninguna capability existente (mismos endpoints, misma autorización, misma auditoría, mismas respuestas). `skip_specs: true` en `.openspec.yaml`.

### New Capabilities
(ninguna)

### Modified Capabilities
(ninguna — sin cambios de comportamiento observable)

## Impact

**Código afectado** (backend, `CarteraProyectos.Core`):
- `Features/WorkItems/*` (TransitionWorkItemStatus, ReorderWorkItems, BulkAssignWorkItemsToSprint) + `Features/Agent/AgentHandlers.cs`, `AgentBacklogHandlers.cs`
- `Features/Projects/*` (CreateProject, UpdateProject, TransitionProjectStatus, AssignTeamToProject, ProjectRisks, ProjectDependencies) + `Features/Agent/AgentProjectsHandlers.cs`, `AgentGovernanceHandlers.cs`
- `Features/Sprints/*` (CreateSprint, TransitionSprintStatus) + `Features/Agent/AgentSprintsEpicsHandlers.cs`
- `Features/Epics/*` (CreateEpic, UpdateEpic) + `Features/Agent/AgentSprintsEpicsHandlers.cs`
- `Features/Persons/*` (GetPersons, UpdatePerson, SetPersonActive) + `Features/Agent/AgentPersonsHandlers.cs`
- Composición de lectura en `Features/Agent/AgentChartHandlers.cs` y `AgentExportHandlers.cs` (dependen hoy de `AgentGetCapacityQuery`/`AgentGetProjectsQuery`/`AgentGetMyTasksQuery` vía `sender.Send`)
- Nuevo proyecto `tests/CarteraProyectos.ArchTests` (no existe todavía pese a estar documentado en `AGENTS.md`)
- `tests/CarteraProyectos.UnitTests`: los tests existentes de cada `Agent*Handler`/handler de dominio afectado necesitan actualizar su setup (inyectar el servicio nuevo, o el fake correspondiente, en vez de un `ISender` mockeado)
- `.ai/skills/dotnet10/SKILL.md` (documentación de la convención)

**No afectado**: frontend Angular, contratos HTTP/OpenAPI, `ChatToolCatalog` (sigue llamando a los mismos `Agent*Command`/`Query` vía `sender.Send` desde `SendChatMessageHandler`, que es el único nivel de indirección que queda), migraciones de BD.
