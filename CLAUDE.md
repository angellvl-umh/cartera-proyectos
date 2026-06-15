# Cartera de Proyectos TIC

Plataforma web universitaria de gestión de cartera de proyectos TIC con integración de agente IA (lenguaje natural via Open WebUI + LiteLLM → AWS Bedrock).

## Stack

| Capa | Tecnología |
|------|-----------|
| Backend | .NET 10, Minimal APIs, MediatR CQRS, EF Core 10 |
| Base de datos | PostgreSQL 18 + pgvector |
| Auth | Keycloak 26 (dev) / SSO SAML2/OAuth universitario (prod) |
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

## Estado actual (2026-06-14)

**Implementado:**
- Infraestructura: Docker Compose (db + keycloak + backend + frontend + litellm + open-webui)
- Backend CRUD completo: Teams, Projects (+ máquina de estados), Persons, Epics, WorkItems, Sprints (+ máquina de estados), Comments
- WorkItems: múltiples asignados, estimación horas + story points, IsHito, DueDate, SprintId
- Frontend Angular 21 con OIDC: Teams, Projects, Kanban por sprint, Kanban global por proyecto, Epics, WorkItems
- Dashboard: panel de control con info usuario, stat cards, gráficos nz-progress, proyectos y sprints activos del usuario
- Informe de proyecto (`/projects/:id/report`): stats, épicas, hitos (timeline), sprints
- Mis tareas (`/my-tasks`): listado cross-proyecto con filtros por estado y contador
- Capacidad (`/capacity`): grid de equipos con carga por persona (Green/Yellow/Red)
- Cartera (`/portfolio`): vista global filtrable por año y estado con stats clickables
- Perfil de persona (`/persons/:id`): datos, equipos, carga de trabajo y tareas activas
- Endpoints API: `/api/dashboard`, `/api/projects/{id}/report`, `/api/me/workitems`, `/api/capacity`, `/api/portfolio`, `/api/persons/{id}/profile`
- **Agente IA (Open WebUI Tool Server):** `GET|POST /api/agent/*` — mis tareas, proyectos, detalle proyecto, capacidad, búsqueda semántica, cambiar estado, crear tarea, añadir comentario, reindexar embeddings
- **Embeddings semánticos:** `IEmbeddingService` → `BedrockEmbeddingService` (amazon.titan-embed-text-v2:0), `WorkItemEmbedding` entity + migración `AddWorkItemEmbeddings`
- **LiteLLM:** proxy OpenAI-compatible → AWS Bedrock; config en `infra/litellm/config.yaml`
- **Open WebUI:** conectado a LiteLLM; Tool Server apunta a `http://backend:8080/api/agent` con API key
- 69 tests unitarios

**Pendiente:** tests E2E Playwright.

---

## Dominio

### Entidades

| Entidad | Campos clave |
|---------|-------------|
| **Person** | Id, SubjectId (SSO sub, unique), Name, Email, Role (Desarrollador/JefeEquipo/Gestor) |
| **Team** | Id, Name, Description?, LeadPersonId? (FK→Person) |
| **Project** | Id, Title, RequestingUnit, Complexity (Low/Medium/High/VeryHigh), Status, PortfolioYear?, StartDate?, EndDate? |
| **Epic** | Id, ProjectId, Title, Priority, SortOrder |
| **WorkItem** | Id, EpicId?, ProjectId?, Title, Status (Backlog/ToDo/InProgress/Blocked/Done), Priority, AssignedToId?, SortOrder, Estimation?, IsHito (bool, default false), HitoDate (DateOnly?) |
| **Comment** | Id, WorkItemId, AuthorId, Text, CreatedAt |

Join tables: `PersonTeamMembership` (PersonId, TeamId, JoinedAt), `ProjectTeamAssignment` (ProjectId, TeamId, IsPrimary).

### Máquinas de estado

**Project:**
```
[*] → Proposed
Proposed → Approved | Cancelled          (solo Gestor)
Approved → InProgress | Cancelled        (Gestor / JefeEquipo del proyecto)
InProgress → Paused | Completed | Cancelled
Paused → InProgress | Cancelled
```

**WorkItem:**
```
Backlog → ToDo → InProgress → Blocked | Done
(cualquier estado no-Done puede retroceder a cualquier estado anterior)
Done es terminal — no puede retroceder. Para reabrir, crear una nueva tarea.
Blocked indica bloqueo temporal; puede avanzar a InProgress o Done.
```

### Permisos por rol

| Acción | Gestor | JefeEquipo | Desarrollador |
|--------|--------|------------|---------------|
| CRUD Projects | ✅ | ❌ | ❌ |
| Aprobar proyectos | ✅ | ❌ | ❌ |
| CRUD Teams/Persons | ✅ | ❌ | ❌ |
| Asignar proyectos a equipos | ✅ | ❌ | ❌ |
| Crear épicas | ✅ | ✅ (sus proyectos) | ❌ |
| Crear tareas | ✅ | ✅ | ✅ |
| Asignar tareas | ✅ | ✅ (equipos del proyecto) | Solo autoasignación |
| Cambiar estado tarea | ✅ | ✅ (equipos del proyecto) | ✅ (propias) |
| Ver tablero Kanban completo | ✅ | ✅ | ✅ (ve todo, arrastra solo las propias) |
| Arrastrar tareas en Kanban | ✅ | ✅ (equipos del proyecto) | Solo propias |
| Ver capacidad | ✅ | ✅ (propios equipos) | ❌ |
| Ver informes | ✅ | ✅ (propios proyectos) | ❌ |

### Terminología (docs ↔ código)

| Término en docs | Código / enum |
|----------------|---------------|
| Gestor de cartera | `PersonRole.Gestor` |
| Jefe de equipo | `PersonRole.JefeEquipo` |
| En ejecución | `ProjectStatus.InProgress` |
| Tareas | `WorkItem` |
| Hito | `WorkItem` con `IsHito = true` |

### Reglas de negocio

1. Una persona puede pertenecer a múltiples equipos simultáneamente
2. Un proyecto puede tener múltiples equipos (uno es primario)
3. Solo personas del equipo del proyecto pueden ser asignadas a sus tareas
4. Provisión automática de usuario desde JWT claims (rol inicial: Desarrollador)
5. Agente IA: permisos del usuario via `X-Open-WebUI-User-Email` header
6. Equipo no puede eliminarse si tiene proyectos activos
7. "JefeEquipo del proyecto" = cualquier JefeEquipo de cualquier equipo asignado al proyecto (no solo el equipo primario)
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

### Flujo típico por feature

```
1. /specifier  → Claude Code genera spec técnica (criterios de aceptación, endpoints, DTOs)
2. /backend-dev → Claude Code lee dominio → genera spec detallada → elige modelo → kiro implementa
3. /frontend-dev → Claude Code lee spec API → genera spec Angular → elige modelo → kiro implementa
4. /tester (opcional) → kiro amplía cobertura de tests
5. /architect (opcional) → Claude Code revisa coherencia de lo implementado
```

Los agentes kiro están definidos en `.kiro/agents/`. Claude Code los invoca desde el directorio raíz del proyecto para que kiro detecte los agentes del workspace.
