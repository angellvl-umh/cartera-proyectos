# Cartera de Proyectos TIC

Plataforma web universitaria de gestión de cartera de proyectos TIC con integración de agente IA (lenguaje natural via Open WebUI + LiteLLM → AWS Bedrock).

## Organización de la programación agéntica

Este proyecto se trabaja indistintamente con Claude Code, kiro-cli y opencode. Para evitar tres copias divergentes de las mismas instrucciones, `.ai/` es la fuente única de verdad:

- `.ai/AGENTS.md` — este fichero: stack, dominio, convenciones (tool-agnostic)
- `.ai/roles/*.md` — definición de cada rol/agente (specifier, backend-dev, frontend-dev, architect, tester)
- `.ai/skills/*/SKILL.md` — skills detallados por tecnología (dotnet10, angular21, domain)

Cada herramienta solo tiene un adaptador fino que referencia `.ai/`, sin duplicar contenido:

| Herramienta | Instrucciones de proyecto | Roles | Skills |
|-------------|---------------------------|-------|--------|
| Claude Code | `CLAUDE.md` → `@AGENTS.md` | `.claude/commands/*.md` → `@.ai/roles/<rol>.md` | `.claude/skills/*/SKILL.md` (symlink a `.ai/skills/`) |
| kiro-cli | — (agentes JSON no soportan import) | `.kiro/agents/*.json` → `resources: ["file://.ai/roles/<rol>.md"]` | `.kiro/skills/*/SKILL.md` (symlink a `.ai/skills/`) |
| opencode | `AGENTS.md` (raíz, symlink a `.ai/AGENTS.md`) | `.opencode/commands/*.md` → `@.ai/roles/<rol>.md` | referenciado desde el propio rol |

**Regla:** nunca edites el contenido de un rol o skill fuera de `.ai/` — edítalo ahí y las tres herramientas quedan sincronizadas automáticamente (symlink o `@import`).

### OpenSpec (specs versionadas)

Requiere el CLI `openspec` en el PATH: `npm install -g --prefix "$HOME/.local" @fission-ai/openspec@latest` (o `npm install -g @fission-ai/openspec@latest` si tienes permisos de escritura en el prefix global de npm). Los comandos `/opsx:*` ejecutan `openspec ...` vía `Bash(openspec:*)` — sin el binario instalado, fallan.

A diferencia de los roles, **no** se porta a mano al patrón `.ai/`: el propio CLI genera y mantiene los adaptadores nativos por herramienta (`openspec init --tools claude,kiro,opencode`, o `--tools all`). Al actualizar de versión, re-ejecutar ese comando para refrescar los tres:

| Herramienta | Comandos | Skills |
|-------------|----------|--------|
| Claude Code | `.claude/commands/opsx/*.md` (`/opsx:propose`, `/opsx:apply`, ...) | `.claude/skills/openspec-*/SKILL.md` |
| kiro-cli | `.kiro/prompts/opsx-*.prompt.md` | `.kiro/skills/openspec-*/SKILL.md` |
| opencode | `.opencode/commands/opsx-*.md` (`/opsx-propose`, `/opsx-apply`, ...) | `.opencode/skills/openspec-*/SKILL.md` |

`/opsx:apply-kiro` es la única excepción — es un comando propio del repo (no generado por OpenSpec), específico de Claude Code, que delega la implementación en kiro-cli siguiendo el modelo cerebro/manos de este documento.

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

.ai/                                 # Fuente única: roles y skills de programación agéntica (ver arriba)
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
| **Project** | Id, Title, Description?, RequestingUnit?, Complexity (VerySmall/Small/Medium/Large/VeryLarge), Status, PortfolioYear?, StartDate?, EndDate?, PreviousReferenceId?, BeneficiaryCount?, PromoterId?, OrganicUnitId?, UorOrder?, GroupPriority?, DesiredDeploymentDate?, SpecificationsUrl?, EpicUrl?, EstimatedBudget? |
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

Skill detallado: `.ai/skills/dotnet10/SKILL.md`

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

Skill detallado: `.ai/skills/angular21/SKILL.md`
