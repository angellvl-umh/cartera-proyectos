# Cartera de Proyectos TIC

Plataforma web universitaria de gestión de cartera de proyectos TIC con integración de agente IA (lenguaje natural via Open WebUI + LiteLLM → AWS Bedrock).

## Stack

| Capa | Tecnología |
|------|-----------|
| Backend | .NET 10, Minimal APIs, MediatR CQRS, EF Core 10 |
| Base de datos | PostgreSQL 18 + pgvector |
| Auth | Keycloak 26 como identity broker: cuentas locales + Google + SSO SAML2 universitario (pendiente de metadata) — ver `infra/KEYCLOAK.md` |
| Frontend | Angular 21 zoneless + signals + standalone, NG-ZORRO 21, Angular CDK |
| IA | Open WebUI + LiteLLM → AWS Bedrock (Claude/Nova) |
| Tests | xUnit + NSubstitute + Shouldly + Testcontainers / Vitest + Playwright |
| Infra | Docker Compose |

## Estructura del proyecto

```
src/
├── CarteraProyectos.Api/            # Minimal APIs (endpoint groups), OpenAPI/Scalar, Middleware
├── CarteraProyectos.Core/           # Domain, Features (CQRS), Interfaces, Common
├── CarteraProyectos.Infrastructure/ # EF Core, Repositorios, pgvector
└── frontend/                        # Angular 21, pnpm

tests/
├── CarteraProyectos.UnitTests/
├── CarteraProyectos.IntegrationTests/
└── CarteraProyectos.ArchTests/

.kiro/skills/                        # Skill files detallados (dotnet10, angular21, domain)
docs/                                # Specs funcionales por módulo
```

## Estado actual (2026-07-02)

**Implementado:**
- Infraestructura: Docker Compose (db + keycloak + backend + frontend + litellm + open-webui)
- Backend CRUD completo: Teams, Projects (+ máquina de estados), Persons, Epics, WorkItems, Sprints (+ máquina de estados), Comments
- WorkItems: múltiples asignados, tipo (Task/UserStory), estimación horas + story points, IsHito, DueDate, SprintId, histórico de estados
- Cartera ampliada: Promoter, OrganicUnit, Tags, ProjectNote, ProjectWeeklyUpdate (semáforo semanal) + informe semanal de cartera
- Auditoría del agente IA (`AgentActionLog` via `AgentAuditBehavior`)
- Frontend Angular 21 con OIDC: Teams, Projects, Kanban por sprint, Kanban global por proyecto, Epics, WorkItems
- Dashboard: panel de control con info usuario, stat cards, gráficos nz-progress, proyectos y sprints activos del usuario
- Informe de proyecto (`/projects/:id/report`): stats, épicas, hitos (timeline), sprints
- Mis tareas (`/my-tasks`): listado cross-proyecto con filtros por estado y contador
- Capacidad (`/capacity`): grid de equipos con carga por persona (Green/Yellow/Red)
- Cartera (`/portfolio`): vista global filtrable por año y estado con stats clickables
- Perfil de persona (`/persons/:id`): datos, equipos, carga de trabajo y tareas activas
- Endpoints API: `/api/dashboard`, `/api/projects/{id}/report`, `/api/me/workitems`, `/api/capacity`, `/api/portfolio`, `/api/persons/{id}/profile`
- **Agente IA (Open WebUI Tool Server):** `GET|POST /api/agent/*` — mis tareas, proyectos, detalle proyecto, capacidad, búsqueda semántica, cambiar estado de tarea (delega en `TransitionWorkItemStatusCommand`: permisos + histórico), crear tarea, comentarios, notas, avance semanal, informe de cartera, reindexar embeddings; CRUD de personas (`get_persons`/`create_person`/`update_person`/`set_person_active`, solo Gestor), `create_project`/`update_project` (solo Gestor; update parcial — solo cambia campos enviados, sin tocar estado ni tags), `update_project_status`, riesgos (`get/add/update_project_risk`) y dependencias (`get/add_project_dependency`) — wrappers `IAgentAuditable` que delegan via `ISender` en los comandos core; usuarios inactivos rechazados (403)
- **Embeddings semánticos:** `IEmbeddingService` → `BedrockEmbeddingService` (amazon.titan-embed-text-v2:0), `WorkItemEmbedding` entity + migración `AddWorkItemEmbeddings`
- **LiteLLM:** proxy OpenAI-compatible → AWS Bedrock; config en `infra/litellm/config.yaml`
- **Open WebUI:** conectado a LiteLLM; Tool Server apunta a `http://backend:8080/api/agent` con API key; SSO OIDC contra Keycloak (cliente `cartera-openwebui`, alta en `pending` + merge por email, `KC_HOSTNAME` fijo con backchannel dinámico) — ver `infra/KEYCLOAK.md`
- **Métricas ágiles:** snapshot de puntos comprometidos (al activar) y entregados (al completar) por sprint (migración `AddSprintPointsSnapshot`); cierre de sprint con carry-over obligatorio si hay tareas sin terminar (a backlog o a otro sprint en Planning); endpoints `GET /api/projects/{id}/velocity`, `GET /api/projects/{projectId}/sprints/{id}/burndown`, `GET /api/projects/{id}/cycle-time` (calculados desde `WorkItemStatusHistory`)
- Gráficos SVG propios (sin librería de charts): `shared/charts/bar-chart` y `line-chart`; velocity, burndown y cycle/lead time en el informe de proyecto; modal de carry-over y comprometido-vs-capacidad en el detalle de proyecto
- **Gobernanza de cartera:** `Project.BusinessValue` (1-5, nz-rate en formulario) + matriz valor/esfuerzo 5×5 en `/portfolio`; entidades `ProjectRisk` (probabilidad × impacto = severidad, estados Open/Mitigated/Closed) y `ProjectDependency` (sin auto/duplicados/ciclos directos) con CRUD y pestañas en detalle (migración `AddRisksDependenciesAndBusinessValue`); `GET /api/portfolio/roadmap` + vista `/roadmap` (timeline anual CSS grid por equipo con hitos); `GET /api/capacity/forecast` + vista `/capacity/forecast` (demanda vs capacidad por trimestre, heurística persona-mes por complejidad en `methodologyNote`)
- **Tests E2E Playwright** en `src/frontend/e2e/` (20 tests: auth OIDC con storageState por rol, proyectos, workitems + descartar, kanban, permisos); corren contra un **stack efímero** (`docker-compose.e2e.yml`: BD en volumen tmpfs `pgdata_e2e`, se descarta al parar) para no contaminar los datos de uso del volumen `pgdata`; comandos `pnpm stack:e2e:up` (levanta + siembra `infra/seed.sql`) / `pnpm e2e` (con `E2E_BASE_URL=http://localhost`) / `pnpm stack:e2e:down` / `pnpm stack:up` (volver al stack de uso); NUNCA `docker compose down -v` (borraría `pgdata`); ver `e2e/README.md`
- **Gestión operativa de tareas de alto nivel:** backlog en componente `product-backlog` (drag&drop para priorizar via `POST .../workitems/reorder`, filtros server-side `q`/épica/prioridad/tipo/asignado, alta rápida inline, selección múltiple → `POST .../workitems/bulk-sprint`, paginación real); drawer compartido `work-item-drawer` (detalle + cambio de estado + comentarios + histórico) abierto desde Kanban y backlog; Kanban con filtro por asignado y "Solo mis tareas" + puntos por columna; Mis tareas con cambio de estado inline; progreso por épica; vista `/team-activity` (`GET /api/teams/activity`): en qué tareas está cada persona de cada equipo, con disponibles
- **CRUD de personas:** alta pre-registrada (se vincula por email en el primer login SSO), edición (nombre/email/rol) y activar/desactivar (`IsActive`, baja lógica; los inactivos se excluyen de listados, capacidad y asignaciones) — `POST|PUT /api/persons`, `PUT /api/persons/{id}/active` (migración `AddPersonCrudFields`: SubjectId nullable)
- **Login sin auto-provisión:** `/api/me` delega en `ResolveCurrentUserCommand` (Core/Features/Users): resuelve por `sub`, vincula pre-registrados por email, y devuelve `403` si la persona no existe o está inactiva (solo excepción: emails de `Admin:InitialGestorEmails` → bootstrap como Gestor); `CurrentUser.ResolveAsync` (helper único, usado también por dashboard/comments/informes) excluye inactivos y hace el mismo fallback de vinculación por email — evita el 401 de las llamadas paralelas a `/api/me` en el primer login; frontend con página `/access-denied` (nz-result 403, oculta sidebar/header) — spec en `docs/spec-login-sin-autoprovision.md`
- **Keycloak identity broker:** persistencia en PostgreSQL (BD `keycloak`, initdb en `infra/docker/initdb/`), IdP de Google en el realm (`GOOGLE_CLIENT_ID/SECRET` por env), placeholder documentado para SAML2 universitario; alta de credenciales locales desde la app: `CreatePersonCommand.CreateLocalCredentials` → `IIdentityProviderService`/`KeycloakAdminService` (cliente `cartera-admin`, Admin API, contraseña temporal con `UPDATE_PASSWORD`; la Person se crea aunque Keycloak falle) — ver `infra/KEYCLOAK.md` y `docs/spec-sso-keycloak-broker.md`
- **Equipos autogestionados:** regla única de autorización `ProjectAuthorization.EnsureCanManageProjectAsync` (Gestor siempre; resto, pertenencia a equipo del proyecto) para transiciones de proyecto/tareas, riesgos, dependencias, notas y semáforo; `PersonRole.JefeEquipo` queda como legacy sin permisos especiales y `Team.LeadPersonId` como dato informativo
- 321+ tests unitarios

### Convención de uso: tareas de ALTO NIVEL

El equipo de desarrollo gestiona el detalle diario en su propia herramienta externa. En esta plataforma los `WorkItem` son **tareas de alto nivel** (entregable o actividad de 1-2 semanas), pensadas para que gestores y jefes de equipo sepan *en qué está cada persona* sin duplicar la gestión fina:

- Granularidad orientativa: si una tarea dura menos de ~3 días, probablemente pertenece a la herramienta del equipo, no a esta cartera
- El enlace al detalle externo se hace a nivel de proyecto (`EpicUrl`, `SpecificationsUrl`)
- Las métricas (velocity, burndown, cycle time, capacidad) se calculan sobre estas tareas gruesas: solo son fiables si el equipo mantiene el estado al día — por eso la UX prioriza el cambio de estado en ≤2 clics (drawer, Mis tareas, Kanban)

---

## Dominio

### Entidades

| Entidad | Campos clave |
|---------|-------------|
| **Person** | Id, SubjectId? (SSO sub, unique; null hasta el primer login si fue pre-registrada), Name, Email (unique), Role (Desarrollador/Gestor; JefeEquipo legacy), IsActive (baja lógica) |
| **Team** | Id, Name, Description?, LeadPersonId? (FK→Person) |
| **Project** | Id, Title, Description?, RequestingUnit?, Complexity (VerySmall/Small/Medium/Large/VeryLarge), Status, PortfolioYear?, StartDate?, EndDate?, PreviousReferenceId?, BeneficiaryCount?, PromoterId?, OrganicUnitId?, UorOrder?, GroupPriority?, SiptGroup?, DesiredDeploymentDate?, SpecificationsUrl?, EpicUrl?, EstimatedBudget? |
| **Epic** | Id, ProjectId, Title, Priority, SortOrder |
| **WorkItem** | Id, ProjectId, EpicId?, SprintId?, Title, Description?, Status (Backlog/ToDo/InProgress/Blocked/Done/Discarded), Priority, Type (Task/UserStory), SortOrder, EstimationHours?, EstimationPoints?, IsHito (bool, default false), HitoDate?, DueDate?, Assignees (N:M con Person) |
| **Sprint** | Id, ProjectId, Name, Goal?, StartDate?, EndDate?, Status (Planning/Active/Completed), Capacity? |
| **Comment** | Id, WorkItemId, AuthorId, Text, CreatedAt |
| **Promoter** / **OrganicUnit** / **Tag** | Catálogos administrables (`/api/promoters`, `/api/organic-units`, `/api/tags`); Tag N:M con Project |
| **ProjectNote** | Id, ProjectId, AuthorId, Text, CreatedAt |
| **ProjectWeeklyUpdate** | Id, ProjectId, AuthorId, WeekOf, Summary, HealthStatus (OnTrack/AtRisk/Blocked) — semáforo semanal por proyecto |
| **WorkItemStatusHistory** / **SprintStatusHistory** | Histórico de transiciones (From, To, ChangedBy, ChangedAt) |
| **AgentActionLog** | Auditoría de acciones del agente IA |

Join tables: `PersonTeamMembership` (PersonId, TeamId, JoinedAt), `ProjectTeamAssignment` (ProjectId, TeamId, IsPrimary).

### Máquinas de estado

**Project** (9 estados operativos; validado en `Project.TransitionTo`, grafo expuesto via `AllowedNextStatuses` en el detalle):
```
Stopped                  → PlanningWithClient | PostponedByClient
PlanningWithClient       → WaitingForDevelopers | PlanningSprint | DevelopmentOutsideSprint
WaitingForDevelopers     → PlanningSprint | DevelopmentOutsideSprint | PlanningWithClient
PlanningSprint           → InSprint | WaitingForDevelopers
InSprint                 → InTesting | PlanningSprint | DevelopmentOutsideSprint
DevelopmentOutsideSprint → InTesting | PlanningSprint
InTesting                → Completed | InSprint | DevelopmentOutsideSprint
PostponedByClient        → PlanningWithClient | PlanningSprint | DevelopmentOutsideSprint
(+ desde cualquier estado no terminal → Stopped | PostponedByClient)
Completed es terminal. Para pasar a Completed: todos los sprints Completed
y todas las tareas Done o Discarded.
```

**WorkItem:**
```
Backlog → ToDo → InProgress → Blocked | Done
(cualquier estado no terminal puede transicionar a cualquier otro)
Done y Discarded son terminales — no pueden retroceder. Para reabrir, crear una nueva tarea.
Discarded = tarea descartada; se excluye de listados de pendientes y NO cuenta como Done en métricas.
Blocked indica bloqueo temporal; puede avanzar a InProgress o Done.
```

**Sprint:**
```
Planning → Active → Completed (terminal)
Solo se puede editar un sprint en Planning.
Para completar un sprint: todas sus tareas Done o Discarded.
```

### Permisos (equipos autogestionados)

**Regla única de autorización** (`ProjectAuthorization.EnsureCanManageProjectAsync`): *el Gestor pasa siempre; cualquier otra persona debe pertenecer (`PersonTeamMembership`) a un equipo asignado al proyecto*. No hay rol de jefe de equipo en la práctica: `PersonRole.JefeEquipo` se conserva en el enum por datos históricos pero no se asigna desde la UI ni otorga permisos especiales. `Team.LeadPersonId` es informativo (contacto), no un permiso.

| Acción | Gestor | Miembro de equipo del proyecto | Ajeno |
|--------|--------|-------------------------------|-------|
| CRUD Projects / aprobar proyectos | ✅ | ❌ | ❌ |
| CRUD Teams/Persons (alta, edición, activar/desactivar, roles) | ✅ | ❌ | ❌ |
| Asignar proyectos a equipos | ✅ | ❌ | ❌ |
| Cambiar estado del proyecto | ✅ | ✅ | ❌ |
| Riesgos y dependencias | ✅ | ✅ | ❌ |
| Notas y semáforo semanal | ✅ | ✅ | ❌ |
| Crear tareas | ✅ | ✅ | ✅ |
| Cambiar estado / arrastrar en Kanban (cualquier tarea del proyecto) | ✅ | ✅ | ❌ |

### Terminología (docs ↔ código)

| Término en docs | Código / enum |
|----------------|---------------|
| Gestor de cartera | `PersonRole.Gestor` |
| Jefe de equipo (legacy, ya no se asigna) | `PersonRole.JefeEquipo` |
| En sprint / En pruebas | `ProjectStatus.InSprint` / `ProjectStatus.InTesting` |
| Proyecto activo | Status ∉ {Stopped, Completed, PostponedByClient} |
| Tareas | `WorkItem` (Type: Task o UserStory) |
| Hito | `WorkItem` con `IsHito = true` |
| Descartada | `WorkItemStatus.Discarded` (terminal) |

### Reglas de negocio

1. Una persona puede pertenecer a múltiples equipos simultáneamente
2. Un proyecto puede tener múltiples equipos (uno es primario)
3. Cualquier persona activa puede ser asignada a una tarea (incluidos Gestores, aunque no pertenezcan al equipo); las personas inactivas no pueden recibir asignaciones
4. **Sin auto-provisión en login**: las personas las pre-registra un Gestor (`POST /api/persons`); en el primer login SSO se vinculan por email (`SubjectId` ← claim `sub`). Si quien hace login no existe como Person o está desactivada → `403` en `/api/me` y pantalla "Sin acceso" (`/access-denied`). Única excepción bootstrap: los emails de `Admin:InitialGestorEmails` se auto-crean como Gestor en su primer login
5. Agente IA: permisos del usuario via `X-Open-WebUI-User-Email` header
6. Equipo no puede eliminarse si tiene proyectos activos
7. Equipos autogestionados: cualquier miembro de un equipo asignado al proyecto puede gestionarlo (regla única de `ProjectAuthorization`); no existe jefe de equipo en la práctica
8. Hitos = WorkItems con `IsHito = true` y `HitoDate` opcional; aparecen en informes agrupados en alcanzados (Done) y próximos (no Done)

---

## Convenciones Backend

- **NUNCA** Controllers — solo Minimal APIs con `MapGroup` por recurso
- **NUNCA** Swashbuckle — solo `Microsoft.AspNetCore.OpenApi` + Scalar (`/scalar`)
- **NUNCA** ASP.NET Identity
- **NUNCA** devolver entidades de dominio desde endpoints — siempre DTOs/records
- **NUNCA** lógica de negocio en los endpoints — siempre en handlers
- Un archivo por caso de uso: `Command + Handler + Validator + DTO` juntos
- Primary constructors para handlers y servicios DI
- Records para Commands, Queries y DTOs
- `CancellationToken` propagado en todos los métodos async
- Enums almacenados como strings en BD
- Descripciones OpenAPI en español (la spec es el Tool Server del agente IA)
- **TODOS los endpoints GET de listado deben ser paginados**: `page` (default 1) y `pageSize` (default 20, máx 100), respuesta `PagedResult<T> { Items, Total, Page, PageSize }`
- **Cada feature debe incluir tests unitarios** en `tests/CarteraProyectos.UnitTests/` usando xUnit + EF InMemory + Shouldly. Mínimo: happy path, not found, reglas de negocio violadas

Skill detallado: `.kiro/skills/dotnet10/SKILL.md`

## Convenciones Frontend

- **NUNCA** NgModules — solo standalone components
- **NUNCA** constructor injection — solo `inject()`
- **NUNCA** `*ngIf`/`*ngFor`/`*ngSwitch` — solo `@if`/`@for`/`@switch`
- **NUNCA** zone.js — ya hay `provideZonelessChangeDetection()` en app.config.ts
- **NUNCA** BehaviorSubject cuando un `signal()` es suficiente
- `ChangeDetectionStrategy.OnPush` en todos los componentes
- Rutas lazy-loaded con `loadComponent`
- NG-ZORRO para todos los componentes UI
- Angular CDK DragDropModule para Kanban
- `toSignal()` para convertir observables de HttpClient a signals

- **Tests E2E con Playwright** para los flujos principales de cada módulo (login, CRUD básico, transición de estado)

Skill detallado: `.kiro/skills/angular21/SKILL.md`

---

## Modelo de orquestación agéntica

**Claude Code = cerebro** (planifica, genera specs, revisa output, toma decisiones de arquitectura)
**kiro-cli = manos** (implementa código, ejecuta builds y tests)

### Uso de herramientas — regla de oro

Preferir siempre la herramienta más barata que resuelva el problema:

| Tarea | Herramienta correcta | ❌ No usar |
|-------|---------------------|-----------|
| Buscar símbolo o archivo | `Grep` / `Glob` directamente | Agent:Explore |
| Leer archivo concreto | `Read` directamente | Agent |
| Implementar código | `kiro-cli` via Bash | Agent con Edit/Write |
| Exploración open-ended (>5 ficheros desconocidos) | `Agent:Explore` — solo si proteger contexto es crítico | — |
| Revisión independiente de código | `Agent:code-reviewer` — solo si se necesita perspectiva externa | — |

**El `Agent` tool crea una instancia Sonnet completa que arranca en frío y re-deriva contexto ya conocido — es la ruta cara. Usarlo solo cuando sea imprescindible.**

### Patrón de invocación

```bash
# Desde el directorio del proyecto (para que kiro detecte los agentes locales)
kiro-cli chat "<spec técnica detallada>" \
  --agent <agent-name> \
  --no-interactive \
  --trust-all-tools \
  --model <modelo-elegido>
```

### Ejecución dentro de Herdr: kiro-cli en panes

Si la sesión corre dentro de [Herdr](https://herdr.dev) (detectable por la variable de entorno `HERDR_ENV=1`; `HERDR_PANE_ID` identifica la pane actual), **lanzar kiro-cli en una pane paralela en lugar de bloquear el Bash de Claude Code**. Así el usuario ve el progreso de kiro en directo y Claude Code queda libre mientras espera.

```bash
# 1. Lanzar kiro en una pane nueva (split a la derecha de la pane actual)
herdr agent start kiro --cwd "C:\Angel\git\cartera-proyectos" --split right --no-focus -- \
  kiro-cli chat "<spec técnica detallada>" --agent <agent-name> --no-interactive --trust-all-tools --model <modelo>
# → devuelve el pane_id de la pane creada

# 2. Esperar a que termine (bloqueante, con timeout generoso)
herdr wait agent-status <pane_id> --status idle --timeout 600000
#    (si herdr no detecta kiro como agente, usar: herdr wait output <pane_id> --match "<texto final>" --timeout 600000)

# 3. Leer el resultado y revisar el output como siempre
herdr pane read <pane_id> --source recent --lines 300

# 4. Cerrar la pane cuando ya no haga falta
herdr pane close <pane_id>
```

Notas:
- Si `HERDR_ENV` no está definida, usar el patrón de invocación directo por Bash de arriba (sin cambios).
- Para builds/tests largos (docker compose, Playwright) aplica el mismo patrón: `herdr pane split` + `herdr pane run` + `herdr wait output`.
- Las specs largas con comillas conflictivas pueden escribirse a un fichero temporal y pasarse con `--resume` o interpolación, según convenga.

### Selección de modelo para kiro-cli

Claude Code elige el modelo en función de la complejidad de la tarea. Al final de cada spec generada se incluye la línea:
> **Modelo recomendado:** `<modelo>` — `<razón en una frase>`

| Señal de complejidad | Modelo |
|----------------------|--------|
| Backend CQRS puro (handler + endpoint siguiendo patrón existente) | `claude-haiku-4.5` |
| Tests unitarios sobre código existente | `claude-haiku-4.5` |
| Frontend con un único componente aislado | `claude-haiku-4.5` |
| Frontend con múltiples archivos y contratos TypeScript entre ellos | `claude-sonnet-4.6` |
| Refactor transversal (cambia interfaces que impactan varios archivos) | `claude-sonnet-4.6` |
| Lógica de negocio compleja o máquina de estados nueva | `claude-sonnet-4.6` |

Referencia de coste relativo: Haiku ≈ 0.4×, Sonnet ≈ 1.3× (escala sobre base 1.0).

IDs de modelo exactos para kiro-cli: `claude-haiku-4.5`, `claude-sonnet-4.6`, `claude-opus-4.6` (punto separador, no guión).

### Slash commands y asignación de roles

| Comando | Claude Code hace | kiro-cli hace | Modelo por defecto |
|---------|-----------------|---------------|-------------------|
| `/specifier` | Genera spec directamente | — | Sonnet (cerebro) |
| `/backend-dev` | Lee contexto, genera spec .NET detallada, elige modelo, llama a kiro, revisa output | Implementa handlers, endpoints, tests, migración | Haiku (ajustable) |
| `/frontend-dev` | Lee API y contexto, genera spec Angular detallada, elige modelo, llama a kiro, revisa output | Implementa componentes, servicios, rutas | Sonnet (ajustable) |
| `/architect` | Revisa código directamente | — | Sonnet (cerebro) |
| `/tester` | Define casos de prueba, elige modelo, llama a kiro, revisa cobertura | Escribe tests unitarios, integración y E2E | Haiku (ajustable) |
| `/opsx:apply-kiro` | Agrupa tasks de un change OpenSpec por capa, elige modelo, llama a kiro (pane Herdr si aplica), revisa output | Implementa las tasks de la capa asignada | Haiku (ajustable, por capa) |

### Flujo típico por feature

```
1. /specifier  → Claude Code genera spec técnica (criterios de aceptación, endpoints, DTOs)
2. /backend-dev → Claude Code lee dominio → genera spec detallada → elige modelo → kiro implementa
3. /frontend-dev → Claude Code lee spec API → genera spec Angular → elige modelo → kiro implementa
4. /tester (opcional) → kiro amplía cobertura de tests
5. /architect (opcional) → Claude Code revisa coherencia de lo implementado
```

Los agentes kiro están definidos en `.kiro/agents/`. Claude Code los invoca desde el directorio raíz del proyecto para que kiro detecte los agentes del workspace.

### OpenSpec como complemento (specs versionadas + delegación a kiro/Herdr)

[OpenSpec](https://github.com/Fission-AI/OpenSpec) (`openspec/`, comandos `/opsx:*` en `.claude/commands/opsx/`) es un complemento opcional al flujo de arriba para features que conviene versionar como artefactos (`proposal.md` / `design.md` / `tasks.md` por change en `openspec/changes/<nombre>/`), en vez de specs efímeras en el chat. No sustituye a `/specifier` — puedes usar `/specifier` para pensar la spec y `/opsx:propose` para dejarla como artefactos versionados, o ir directo a `/opsx:propose`.

```
1. /opsx:propose "<idea>" → Claude Code crea el change y genera proposal/design/tasks
2. /opsx:apply-kiro <change> → Claude Code agrupa las tasks por capa (Backend/Frontend/Tests),
                                 elige modelo por capa y delega en kiro-cli (pane Herdr si HERDR_ENV=1),
                                 revisa build/tests/diff antes de marcar cada task como hecha
3. /opsx:archive <change>  → una vez todas las tasks están completas y verificadas
```

`/opsx:apply` (generado por OpenSpec, sin sufijo `-kiro`) sigue disponible tal cual: hace que Claude Code implemente directamente, útil para changes pequeños o fuera de Herdr. `/opsx:apply-kiro` es la variante que respeta el modelo cerebro/manos de esta sección — úsala por defecto cuando el change vaya a tocar código real.
