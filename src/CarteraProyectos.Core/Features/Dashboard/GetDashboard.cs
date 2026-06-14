using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.Dashboard;

public record GetDashboardQuery(int PersonId) : IRequest<DashboardDto>;

public record DashboardDto(
    DashboardMeDto Me,
    IReadOnlyList<DashboardProjectDto> MyProjects,
    IReadOnlyList<DashboardSprintDto> ActiveSprints,
    WorkItemStatsDto MyWorkItems);

public record DashboardMeDto(int Id, string Name, string Email, string Role);

public record DashboardProjectDto(
    int Id, string Title, string Status, string RequestingUnit,
    string? StartDate, string? EndDate,
    int TotalWorkItems, int DoneWorkItems);

public record DashboardSprintDto(
    int Id, int ProjectId, string ProjectTitle,
    string Name, string? Goal, string? StartDate, string? EndDate,
    int WorkItemCount, int DoneWorkItems, int TotalEstimationPoints);

public record WorkItemStatsDto(
    int Total, int Backlog, int ToDo, int InProgress, int Blocked, int Done,
    int Critical, int High, int Medium, int Low);

public sealed class GetDashboardHandler(IAppDbContext db)
    : IRequestHandler<GetDashboardQuery, DashboardDto>
{
    public async Task<DashboardDto> Handle(GetDashboardQuery request, CancellationToken ct)
    {
        var me = await db.Persons.FindAsync([request.PersonId], ct)
            ?? throw new KeyNotFoundException($"Person {request.PersonId} not found.");

        var myTeamIds = await db.PersonTeamMemberships
            .Where(m => m.PersonId == request.PersonId)
            .Select(m => m.TeamId)
            .ToListAsync(ct);

        var myProjectIds = await db.ProjectTeamAssignments
            .Where(a => myTeamIds.Contains(a.TeamId))
            .Select(a => a.ProjectId)
            .Distinct()
            .ToListAsync(ct);

        var myProjects = await db.Projects
            .Where(p => myProjectIds.Contains(p.Id))
            .OrderByDescending(p => p.Id)
            .Select(p => new DashboardProjectDto(
                p.Id, p.Title, p.Status.ToString(), p.RequestingUnit,
                p.StartDate != null ? p.StartDate.Value.ToString() : null,
                p.EndDate != null ? p.EndDate.Value.ToString() : null,
                db.WorkItems.Count(w => w.ProjectId == p.Id),
                db.WorkItems.Count(w => w.ProjectId == p.Id && w.Status == WorkItemStatus.Done)))
            .ToListAsync(ct);

        var activeSprints = await db.Sprints
            .Where(s => myProjectIds.Contains(s.ProjectId) && s.Status == SprintStatus.Active)
            .OrderBy(s => s.EndDate)
            .Select(s => new DashboardSprintDto(
                s.Id, s.ProjectId, s.Project!.Title,
                s.Name, s.Goal,
                s.StartDate != null ? s.StartDate.Value.ToString() : null,
                s.EndDate != null ? s.EndDate.Value.ToString() : null,
                s.WorkItems.Count,
                s.WorkItems.Count(w => w.Status == WorkItemStatus.Done),
                s.WorkItems.Sum(w => w.EstimationPoints ?? 0)))
            .ToListAsync(ct);

        var myWorkItemStatuses = await db.WorkItems
            .Where(w => myProjectIds.Contains(w.ProjectId) && w.Assignees.Any(a => a.Id == request.PersonId))
            .Select(w => new { w.Status, w.Priority })
            .ToListAsync(ct);

        var stats = new WorkItemStatsDto(
            Total:      myWorkItemStatuses.Count,
            Backlog:    myWorkItemStatuses.Count(w => w.Status == WorkItemStatus.Backlog),
            ToDo:       myWorkItemStatuses.Count(w => w.Status == WorkItemStatus.ToDo),
            InProgress: myWorkItemStatuses.Count(w => w.Status == WorkItemStatus.InProgress),
            Blocked:    myWorkItemStatuses.Count(w => w.Status == WorkItemStatus.Blocked),
            Done:       myWorkItemStatuses.Count(w => w.Status == WorkItemStatus.Done),
            Critical:   myWorkItemStatuses.Count(w => w.Priority == WorkItemPriority.Critical),
            High:       myWorkItemStatuses.Count(w => w.Priority == WorkItemPriority.High),
            Medium:     myWorkItemStatuses.Count(w => w.Priority == WorkItemPriority.Medium),
            Low:        myWorkItemStatuses.Count(w => w.Priority == WorkItemPriority.Low));

        return new DashboardDto(
            new DashboardMeDto(me.Id, me.Name, me.Email, me.Role.ToString()),
            myProjects,
            activeSprints,
            stats);
    }
}
