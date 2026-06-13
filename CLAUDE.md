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

## Estado actual (2026-06-13)

**Implementado:** Docker Compose completo (db, keycloak, backend, frontend), entidad `Person`, `AppDbContext`, migración `InitialCreate`, endpoint `GET /api/me` con provisión automática de usuario, frontend Angular 21 con OIDC y dashboard básico.

**Pendiente:** todo el dominio restante (Team, Project, Epic, WorkItem, Comment), MediatR, FluentValidation, pgvector, todos los endpoints CRUD, todos los features de frontend, tests.

---

## Dominio

### Entidades

| Entidad | Campos clave |
|---------|-------------|
| **Person** | Id, SubjectId (SSO sub, unique), Name, Email, Role (Desarrollador/JefeEquipo/Gestor) |
| **Team** | Id, Name, Description?, LeadPersonId? (FK→Person) |
| **Project** | Id, Title, RequestingUnit, Complexity (Low/Medium/High/VeryHigh), Status, PortfolioYear?, StartDate?, EndDate? |
| **Epic** | Id, ProjectId, Title, Priority, SortOrder |
| **WorkItem** | Id, EpicId?, ProjectId?, Title, Status (Backlog/ToDo/InProgress/InReview/Done), Priority, AssignedToId?, SortOrder, Estimation?, IsHito (bool, default false), HitoDate (DateOnly?) |
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
Backlog → ToDo → InProgress → InReview → Done
(cualquier estado no-Done puede retroceder a cualquier estado anterior)
Done es terminal — no puede retroceder. Para reabrir, crear una nueva tarea.
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

Skill detallado: `.kiro/skills/angular21/SKILL.md`

---

## Agentes (slash commands)

| Comando | Rol | Modifica código |
|---------|-----|-----------------|
| `/specifier` | Convierte ideas en specs técnicas con criterios de aceptación | ❌ |
| `/backend-dev` | Implementa features .NET 10 (Clean Arch, MediatR, Minimal APIs, tests) | ✅ |
| `/frontend-dev` | Implementa features Angular 21 (signals, zoneless, NG-ZORRO, tests) | ✅ |
| `/architect` | Revisa código y valida arquitectura, emite reporte | ❌ |
| `/tester` | Escribe tests unitarios, integración y E2E | ✅ |

Equivalente al sistema de agentes Kiro en `.kiro/agents/` pero invocados como slash commands de Claude Code.
