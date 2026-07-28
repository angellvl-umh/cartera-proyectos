---
name: project-domain-skill
description: Domain knowledge for Cartera de Proyectos TIC - entities, roles, business rules, status machines. Use when reasoning about requirements, designing features, or validating business logic.
---

# Cartera de Proyectos TIC — Domain Knowledge

## Context
University IT project portfolio management platform. Manages projects, teams, people, epics, tasks (work items), sprints, catalogs (promoters, organic units, tags), and provides Kanban boards, capacity dashboards, reports, semantic search, and AI agent integration.

## Entities

### Person
| Field | Type | Notes |
|-------|------|-------|
| Id | int | PK |
| SubjectId | string? | SSO claim `sub`, unique. Null for pre-registered persons who have never logged in; linked by email on first login |
| Name | string | Required |
| Email | string | Unique |
| Role | enum | Gestor / JefeEquipo / Desarrollador. **JefeEquipo is legacy**: kept for historical data but no longer assigned from the UI and grants no special permissions |
| IsActive | bool | Default true. Inactive persons are excluded from listings, capacity and task assignment (soft delete) |

Persons are created two ways: auto-provisioned from SSO claims on first login, or pre-registered by a Gestor (CRUD at `/api/persons`) and linked to their SSO account by email on first login.

### Team
| Field | Type | Notes |
|-------|------|-------|
| Id | int | PK |
| Name | string | Required, unique |
| Description | string? | |
| LeadPersonId | int? | FK → Person (any person; informational/contact only, grants no permissions) |

### PersonTeamMembership
| Field | Type | Notes |
|-------|------|-------|
| PersonId | int | FK → Person |
| TeamId | int | FK → Team |
| JoinedAt | date | |

### Promoter
| Field | Type | Notes |
|-------|------|-------|
| Id | int | PK |
| Name | string | Required, max 200, unique by convention |

Catalog entity. CRUD is Gestor-only (`/api/promoters`). Cannot be deleted while referenced by a Project.

### OrganicUnit
| Field | Type | Notes |
|-------|------|-------|
| Id | int | PK |
| Name | string | Required, max 200 |
| Code | string? | max 50 |

Catalog entity. CRUD is Gestor-only (`/api/organic-units`). Replaces the old free-text `RequestingUnit` semantically for new projects (Project still keeps `RequestingUnit` as a nullable legacy field). Cannot be deleted while referenced by a Project.

### Tag
| Field | Type | Notes |
|-------|------|-------|
| Id | int | PK |
| Name | string | Required, max 100 |
| Color | string? | max 20, hex color |

Catalog entity. CRUD is Gestor-only (`/api/tags`). Joined to Project via `ProjectTags`. Deleting a tag detaches it from all projects automatically.

### Project
| Field | Type | Notes |
|-------|------|-------|
| Id | int | PK |
| Title | string | Required, max 150 |
| Description | string? | |
| RequestingUnit | string? | Legacy free-text field, kept nullable for old data; new projects use `OrganicUnitId` |
| Complexity | enum | VerySmall / Small / Medium / Large / VeryLarge |
| Status | enum | Stopped / PlanningWithClient / WaitingForDevelopers / PlanningSprint / InSprint / DevelopmentOutsideSprint / InTesting / Completed / PostponedByClient |
| PortfolioYear | int? | null = out of portfolio |
| StartDate | DateOnly? | |
| EndDate | DateOnly? | |
| PreviousReferenceId | int? | ID in a previous/legacy portfolio system |
| BeneficiaryCount | int? | Number of users benefited |
| PromoterId | int? | FK → Promoter |
| OrganicUnitId | int? | FK → OrganicUnit |
| UorOrder | int? | Internal priority order set by the requesting unit |
| GroupPriority | int? | Institutional strategic priority |
| DesiredDeploymentDate | DateOnly? | Target go-live date |
| SpecificationsUrl | string? | max 500, link to specs doc |
| EpicUrl | string? | max 500, link to external epic/Jira |
| Tags | Tag[] | Many-to-many via `ProjectTags` |
| Notes | ProjectNote[] | Follow-up log |

### ProjectTeamAssignment
| Field | Type | Notes |
|-------|------|-------|
| ProjectId | int | FK → Project |
| TeamId | int | FK → Team |
| IsPrimary | bool | One team can be primary |

### ProjectNote
| Field | Type | Notes |
|-------|------|-------|
| Id | int | PK |
| ProjectId | int | FK → Project |
| AuthorId | int | FK → Person |
| Text | string | Required |
| CreatedAt | DateTimeOffset | |

Project-level follow-up log (decisions, milestones, blockers). Created via UI or via the AI agent (`add_project_note`). Distinct from `Comment`, which is attached to a `WorkItem`.

### Epic
| Field | Type | Notes |
|-------|------|-------|
| Id | int | PK |
| ProjectId | int | FK → Project |
| Title | string | Required |
| Description | string? | |
| Priority | int | |
| SortOrder | int | |

### Sprint
| Field | Type | Notes |
|-------|------|-------|
| Id | int | PK |
| ProjectId | int | FK → Project |
| Status | enum | Planning / Active / Completed |

Sprint **does** have a strict finite state machine (see below) — unlike Project status, which is free-form.

### WorkItem
| Field | Type | Notes |
|-------|------|-------|
| Id | int | PK |
| ProjectId | int | FK → Project (required) |
| EpicId | int? | FK → Epic (null = project backlog without epic) |
| SprintId | int? | FK → Sprint (null = not assigned to a sprint) |
| Title | string | Required |
| Description | string? | |
| Status | enum | Backlog / ToDo / InProgress / Blocked / Done |
| Priority | enum | Low / Medium / High / Critical |
| SortOrder | int | |
| EstimationHours | int? | Hour-based estimate |
| EstimationPoints | int? | Story-point estimate (separate from hours, not free text) |
| IsHito | bool | Marks the task as a milestone; default false |
| HitoDate | DateOnly? | Target date for the milestone (only meaningful when IsHito = true) |
| DueDate | DateOnly? | Due date for the task |
| Assignees | Person[] | **Many-to-many** — a task can have multiple assigned people, not a single `AssignedToId` |

### WorkItemEmbedding
| Field | Type | Notes |
|-------|------|-------|
| WorkItemId | int | FK → WorkItem |
| Embedding | vector | pgvector embedding generated from title+description, used for semantic search |

### Comment
| Field | Type | Notes |
|-------|------|-------|
| Id | int | PK |
| WorkItemId | int | FK → WorkItem |
| AuthorId | int | FK → Person |
| Text | string | Required |
| CreatedAt | datetime | |

## State Machines

### Project Status — free-form, role-gated only
Project status has **no** finite state machine: any status can transition to any other status. `Project.TransitionTo(next)` performs no validation on the value itself. Authorization is the only gate:

| Who | Allowed |
|-----|---------|
| Gestor | Always allowed |
| Anyone else | Allowed only if they belong (PersonTeamMembership) to a team assigned to the project |

### Sprint Status — genuine FSM
```
Planning → Active
Active → Completed
Completed → (terminal, no further transitions)
```

### WorkItem Status
```
Backlog → ToDo → InProgress → Blocked / Done
(any non-Done state can move back to any earlier state)
Done is terminal — a task in Done cannot go back. Reopen by creating a new task.
```

## Roles & Permissions

> **Self-managed teams**: there is no team-lead role in practice. The single authorization rule for project management actions is: *Gestor always passes; anyone else must belong to a team assigned to the project* (`ProjectAuthorization.EnsureCanManageProjectAsync`). `PersonRole.JefeEquipo` still exists in the enum for historical data but grants nothing special.

| Action | Gestor | Project team member | Non-member |
|--------|--------|---------------------|------------|
| CRUD Projects | ✅ | ❌ | ❌ |
| Change project status | ✅ | ✅ | ❌ |
| Assign projects to teams | ✅ | ❌ | ❌ |
| CRUD Teams/Persons | ✅ | ❌ | ❌ |
| CRUD Promoters/OrganicUnits/Tags | ✅ | ❌ | ❌ |
| Assign roles / activate-deactivate persons | ✅ | ❌ | ❌ |
| Add project notes / weekly health updates | ✅ | ✅ | ❌ |
| Manage risks & dependencies | ✅ | ✅ | ❌ |
| Create tasks | ✅ | ✅ | ✅ |
| Change task status / drag in Kanban (any task of the project) | ✅ | ✅ | ❌ |
| View capacity / reports | ✅ | ✅ | ✅ |

## Terminology (docs ↔ code)

| Term in docs (Spanish) | Code / SKILL.md |
|------------------------|-----------------|
| Gestor de cartera | `PersonRole.Gestor` |
| Jefe de equipo | `PersonRole.JefeEquipo` |
| Desarrollador | `PersonRole.Desarrollador` |
| Parado | `ProjectStatus.Stopped` |
| Planificando con cliente | `ProjectStatus.PlanningWithClient` |
| Esperando desarrolladores | `ProjectStatus.WaitingForDevelopers` |
| Planificando sprint | `ProjectStatus.PlanningSprint` |
| En sprint | `ProjectStatus.InSprint` |
| Desarrollo fuera de sprint | `ProjectStatus.DevelopmentOutsideSprint` |
| En pruebas | `ProjectStatus.InTesting` |
| Finalizado | `ProjectStatus.Completed` |
| Pospuesto por cliente | `ProjectStatus.PostponedByClient` |
| Muy pequeño / Pequeño / Medio / Grande / Muy grande | `ProjectComplexity.VerySmall/Small/Medium/Large/VeryLarge` |
| Tareas | `WorkItem` |
| Hito | `WorkItem` with `IsHito = true` |

## Business Rules

1. A person can belong to multiple teams simultaneously
2. A project can be assigned to multiple teams (one is primary)
3. Team cannot be deleted if it has active projects
4. WorkItems always belong to a Project; `EpicId` is nullable (project backlog without epic) and `SprintId` is nullable (not yet planned into a sprint)
5. Any active person can be assigned to a task (including Gestores who don't belong to the project's teams); inactive persons cannot receive assignments. A task can have multiple assignees
6. User provisioning is automatic from SSO JWT claims (sub, name, email)
7. Default role on first login: Desarrollador
8. AI agent actions respect user permissions via X-Open-WebUI-User-Email header
9. Semantic search uses pgvector embeddings over WorkItems (generated async via Bedrock Titan embeddings, graceful degradation if unavailable)
10. Project status changes are free-form (any value → any value); only role authorization is checked, there is no transition table
11. Sprint status follows a strict one-directional FSM: Planning → Active → Completed
12. Promoters/OrganicUnits/Tags cannot be deleted while referenced by a Project (Promoters, OrganicUnits) or are silently detached on delete (Tags)
13. ProjectNotes and Comments are append-only follow-up logs (no edit/delete by other users besides author or Gestor)

## API Structure
```
/api/projects          - CRUD projects, status changes, team assignment, notes (/api/projects/{id}/notes)
/api/promoters         - CRUD promoters (Gestor only)
/api/organic-units     - CRUD organic units (Gestor only)
/api/tags              - CRUD tags (Gestor only)
/api/teams             - CRUD teams, membership
/api/persons           - CRUD persons, role management, profile (/api/persons/{id}/profile)
/api/epics             - CRUD epics within projects
/api/workitems         - CRUD tasks, status changes, assignment
/api/sprints           - CRUD sprints, status transitions
/api/comments          - Comments on work items
/api/dashboard         - User dashboard summary
/api/capacity          - Team/person workload
/api/portfolio         - Global portfolio view, filterable
/api/me/workitems      - Current user's cross-project tasks
/api/agent             - AI agent Tool Server (me, projects, project detail, capacity, search, status, create task, comment, project notes, reindex, charts)
```
