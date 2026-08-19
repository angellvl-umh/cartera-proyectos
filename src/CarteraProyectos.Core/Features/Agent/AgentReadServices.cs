using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.Agent;

// ─── Interfaces ───────────────────────────────────────────────────────────────

public interface ICapacityReadService
{
    Task<AgentCapacityDto> GetAsync(CancellationToken ct);
}

public interface IProjectsReadService
{
    Task<IReadOnlyList<AgentProjectSummaryDto>> GetAsync(int personId, string? status, CancellationToken ct);
}

public interface IMyTasksReadService
{
    Task<AgentMyTasksDto> GetAsync(int personId, CancellationToken ct);
}

// ─── Implementations ─────────────────────────────────────────────────────────

public sealed class CapacityReadService(IAppDbContext db) : ICapacityReadService
{
    public async Task<AgentCapacityDto> GetAsync(CancellationToken ct)
    {
        var teams = await db.Teams
            .Include(t => t.Members).ThenInclude(m => m.Person)
            .Include(t => t.Projects).ThenInclude(pta => pta.Project)
            .OrderBy(t => t.Name)
            .ToListAsync(ct);

        var activeStatuses = new[]
        {
            ProjectStatus.PlanningWithClient, ProjectStatus.PlanningSprint,
            ProjectStatus.InSprint, ProjectStatus.DevelopmentOutsideSprint, ProjectStatus.InTesting,
        };
        var result = new List<AgentTeamCapacityDto>();

        foreach (var team in teams)
        {
            var activeProjectCount = team.Projects.Count(pta => pta.Project != null && activeStatuses.Contains(pta.Project.Status));
            var members = new List<AgentMemberCapacityDto>();

            foreach (var m in team.Members)
            {
                if (m.Person is null || !m.Person.IsActive) continue;
                var activeTasks = await db.WorkItems.CountAsync(
                    w => w.Assignees.Any(a => a.Id == m.Person.Id) &&
                         (w.Status == WorkItemStatus.InProgress || w.Status == WorkItemStatus.Blocked), ct);
                var pendingTasks = await db.WorkItems.CountAsync(
                    w => w.Assignees.Any(a => a.Id == m.Person.Id) &&
                         (w.Status == WorkItemStatus.ToDo || w.Status == WorkItemStatus.Backlog), ct);
                var load = activeTasks <= 3 ? "Green" : activeTasks <= 6 ? "Yellow" : "Red";
                members.Add(new AgentMemberCapacityDto(m.Person.Name, m.Person.Role.ToString(), activeTasks, pendingTasks, load));
            }

            result.Add(new AgentTeamCapacityDto(team.Id, team.Name, activeProjectCount,
                members.OrderByDescending(m => m.ActiveTasks).ToList()));
        }

        return new AgentCapacityDto(result);
    }
}

public sealed class ProjectsReadService(IAppDbContext db) : IProjectsReadService
{
    public async Task<IReadOnlyList<AgentProjectSummaryDto>> GetAsync(int personId, string? status, CancellationToken ct)
    {
        var person = await db.Persons.FindAsync([personId], ct)
            ?? throw new KeyNotFoundException("Persona no encontrada.");

        IQueryable<Project> baseQuery;

        if (person.Role == PersonRole.Gestor)
        {
            // Gestor ve toda la cartera
            baseQuery = db.Projects;
        }
        else
        {
            // Resto de roles: solo proyectos de sus equipos
            var myTeamIds = await db.PersonTeamMemberships
                .Where(m => m.PersonId == personId)
                .Select(m => m.TeamId)
                .ToListAsync(ct);

            var myProjectIds = await db.ProjectTeamAssignments
                .Where(a => myTeamIds.Contains(a.TeamId))
                .Select(a => a.ProjectId)
                .Distinct()
                .ToListAsync(ct);

            baseQuery = db.Projects.Where(p => myProjectIds.Contains(p.Id));
        }

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<ProjectStatus>(status, out var parsedStatus))
            baseQuery = baseQuery.Where(p => p.Status == parsedStatus);

        var projects = await baseQuery
            .Select(p => new
            {
                p.Id, p.Title, p.Status, p.RequestingUnit,
                PrimaryTeamName = db.ProjectTeamAssignments
                    .Where(a => a.ProjectId == p.Id && a.IsPrimary)
                    .Select(a => a.Team.Name)
                    .FirstOrDefault(),
                TotalTasks    = db.WorkItems.Count(w => w.ProjectId == p.Id),
                DoneTasks     = db.WorkItems.Count(w => w.ProjectId == p.Id && w.Status == WorkItemStatus.Done),
                ActiveSprints = db.Sprints.Count(s => s.ProjectId == p.Id && s.Status == SprintStatus.Active),
            })
            .OrderByDescending(p => p.Id)
            .ToListAsync(ct);

        return projects.Select(p => new AgentProjectSummaryDto(
            p.Id, p.Title, p.Status.ToString(), p.RequestingUnit, p.PrimaryTeamName,
            p.TotalTasks, p.DoneTasks, p.ActiveSprints)).ToList();
    }
}

public sealed class MyTasksReadService(IAppDbContext db) : IMyTasksReadService
{
    public async Task<AgentMyTasksDto> GetAsync(int personId, CancellationToken ct)
    {
        var person = await db.Persons.FindAsync([personId], ct)
            ?? throw new KeyNotFoundException("Persona no encontrada.");

        var myWorkItems = await db.WorkItems
            .Where(w => w.Assignees.Any(a => a.Id == personId))
            .Select(w => new
            {
                w.Id, w.Title, w.Status, w.Priority,
                ProjectTitle = w.Project != null ? w.Project.Title : string.Empty,
                FirstAssignee = w.Assignees.FirstOrDefault() != null ? w.Assignees.First().Name : null,
                w.EstimationHours, w.EstimationPoints, w.DueDate,
            })
            .ToListAsync(ct);

        var active = myWorkItems
            .Where(w => w.Status == WorkItemStatus.InProgress || w.Status == WorkItemStatus.Blocked || w.Status == WorkItemStatus.ToDo)
            .OrderBy(w => w.Priority == WorkItemPriority.Critical ? 0 : w.Priority == WorkItemPriority.High ? 1 : 2)
            .Select(w => new AgentTaskSummaryDto(w.Id, w.Title, w.Status.ToString(), w.Priority.ToString(),
                w.FirstAssignee, w.EstimationHours, w.EstimationPoints, w.DueDate?.ToString()))
            .ToList();

        var backlog = myWorkItems
            .Where(w => w.Status == WorkItemStatus.Backlog)
            .Select(w => new AgentTaskSummaryDto(w.Id, w.Title, w.Status.ToString(), w.Priority.ToString(),
                w.FirstAssignee, w.EstimationHours, w.EstimationPoints, w.DueDate?.ToString()))
            .ToList();

        return new AgentMyTasksDto(person.Name, person.Role.ToString(), active, backlog,
            myWorkItems.Count(w => w.Status == WorkItemStatus.Done));
    }
}
