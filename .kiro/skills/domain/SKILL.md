---
name: project-domain-skill
description: Domain knowledge for Cartera de Proyectos TIC - entities, roles, business rules, status machines. Use when reasoning about requirements, designing features, or validating business logic.
---

# Cartera de Proyectos TIC — Domain Knowledge

## Context
University IT project portfolio management platform. Manages projects, teams, people, epics, tasks (work items), and provides Kanban boards, capacity dashboards, reports, semantic search, and AI agent integration.

## Entities

### Person
| Field | Type | Notes |
|-------|------|-------|
| Id | int | PK |
| SubjectId | string | SSO claim `sub`, unique |
| Name | string | Required |
| Email | string | Unique, from SSO |
| Role | enum | Gestor / JefeEquipo / Desarrollador |

### Team
| Field | Type | Notes |
|-------|------|-------|
| Id | int | PK |
| Name | string | Required, unique |
| Description | string? | |
| LeadPersonId | int? | FK → Person (must have Role ≥ JefeEquipo) |

### PersonTeamMembership
| Field | Type | Notes |
|-------|------|-------|
| PersonId | int | FK → Person |
| TeamId | int | FK → Team |
| JoinedAt | date | |

### Project
| Field | Type | Notes |
|-------|------|-------|
| Id | int | PK |
| Title | string | Required |
| Description | string? | |
| RequestingUnit | string | Required |
| Complexity | enum | Low / Medium / High / VeryHigh |
| Status | enum | Proposed / Approved / InProgress / Paused / Completed / Cancelled |
| PortfolioYear | int? | null = out of portfolio |
| StartDate | DateOnly? | |
| EndDate | DateOnly? | |

### ProjectTeamAssignment
| Field | Type | Notes |
|-------|------|-------|
| ProjectId | int | FK → Project |
| TeamId | int | FK → Team |
| IsPrimary | bool | One team can be primary |

### Epic
| Field | Type | Notes |
|-------|------|-------|
| Id | int | PK |
| ProjectId | int | FK → Project |
| Title | string | Required |
| Description | string? | |
| Priority | int | |
| SortOrder | int | |

### WorkItem
| Field | Type | Notes |
|-------|------|-------|
| Id | int | PK |
| EpicId | int? | FK → Epic (null = backlog general) |
| ProjectId | int? | FK → Project (null = backlog general) |
| Title | string | Required |
| Description | string? | |
| Status | enum | Backlog / ToDo / InProgress / InReview / Done |
| Priority | int | |
| AssignedToId | int? | FK → Person |
| SortOrder | int | |
| Estimation | string? | Free text (e.g. "3d", "8h") |
| IsHito | bool | Marks the task as a milestone; default false |
| HitoDate | DateOnly? | Target date for the milestone (only meaningful when IsHito = true) |

### Comment
| Field | Type | Notes |
|-------|------|-------|
| Id | int | PK |
| WorkItemId | int | FK → WorkItem |
| AuthorId | int | FK → Person |
| Text | string | Required |
| CreatedAt | datetime | |

## State Machines

### Project Status
```
[*] → Proposed
Proposed → Approved (Gestor only)
Proposed → Cancelled (Gestor only)
Approved → InProgress (Gestor, JefeEquipo of project)
Approved → Cancelled (Gestor only)
InProgress → Paused (Gestor, JefeEquipo of project)
InProgress → Completed (Gestor, JefeEquipo of project)
InProgress → Cancelled (Gestor only)
Paused → InProgress (Gestor, JefeEquipo of project)
Paused → Cancelled (Gestor only)
```

### WorkItem Status
```
Backlog → ToDo → InProgress → InReview → Done
(any non-Done state can move back to any earlier state)
Done is terminal — a task in Done cannot go back. Reopen by creating a new task.
```

## Roles & Permissions

> **"JefeEquipo of a project"** means any JefeEquipo who belongs to any team assigned to that project (not just the primary team). Each JefeEquipo manages all tasks of the project's teams, not only those of their own team.

| Action | Gestor | JefeEquipo | Desarrollador |
|--------|--------|------------|---------------|
| CRUD Projects | ✅ | ❌ | ❌ |
| Approve projects | ✅ | ❌ | ❌ |
| Assign projects to teams | ✅ | ❌ | ❌ |
| CRUD Teams/Persons | ✅ | ❌ | ❌ |
| Assign roles | ✅ | ❌ | ❌ |
| Create epics | ✅ | ✅ (projects where they are JefeEquipo) | ❌ |
| Create tasks | ✅ | ✅ | ✅ |
| Assign tasks | ✅ | ✅ (project's teams) | Self-assign only |
| Change task status | ✅ | ✅ (project's teams) | ✅ (own tasks) |
| View Kanban (full board) | ✅ | ✅ | ✅ (can view all, drag only own) |
| Drag tasks in Kanban | ✅ | ✅ (project's teams) | Own tasks only |
| View capacity | ✅ | ✅ (own teams) | ❌ |
| Generate reports | ✅ | ✅ (own projects) | ❌ |

## Terminology (docs ↔ code)

| Term in docs (Spanish) | Code / SKILL.md |
|------------------------|-----------------|
| Gestor de cartera | `PersonRole.Gestor` |
| Jefe de equipo | `PersonRole.JefeEquipo` |
| Desarrollador | `PersonRole.Desarrollador` |
| Propuesto | `ProjectStatus.Proposed` |
| Aprobado | `ProjectStatus.Approved` |
| En ejecución | `ProjectStatus.InProgress` |
| Pausado | `ProjectStatus.Paused` |
| Completado | `ProjectStatus.Completed` |
| Cancelado | `ProjectStatus.Cancelled` |
| Tareas | `WorkItem` |
| Hito | `WorkItem` with `IsHito = true` |

## Business Rules

1. A person can belong to multiple teams simultaneously
2. A project can be assigned to multiple teams (one is primary)
3. Team cannot be deleted if it has active projects
4. WorkItems without Project/Epic belong to "backlog general"
5. Only persons in a project's team can be assigned tasks of that project
6. User provisioning is automatic from SSO JWT claims (sub, name, email)
7. Default role on first login: Desarrollador
8. AI agent actions respect user permissions via X-Open-WebUI-User-Email header
9. Semantic search uses pgvector embeddings (generated async, graceful degradation)
10. Reports exportable to PDF (QuestPDF) and Excel (ClosedXML)

## API Structure
```
/api/projects      - CRUD projects, status changes, team assignment
/api/teams         - CRUD teams, membership
/api/persons       - CRUD persons, role management
/api/epics         - CRUD epics within projects
/api/workitems     - CRUD tasks, status changes, assignment
/api/backlog       - General backlog (tasks without project)
/api/capacity      - Team/person workload
/api/reports       - Report generation
/api/agent         - Aggregated queries for AI (search, summaries)
```
