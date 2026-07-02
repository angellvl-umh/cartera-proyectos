using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.Persons;

public record GetPersonProfileQuery(int PersonId) : IRequest<PersonProfileDto>;

public record PersonProfileDto(
    int Id, string Name, string Email, string Role,
    IReadOnlyList<PersonTeamDto> Teams,
    PersonWorkloadDto Workload,
    IReadOnlyList<PersonTaskDto> ActiveTasks);

public record PersonTeamDto(int TeamId, string TeamName, bool IsLead);

public record PersonWorkloadDto(
    int Total, int Backlog, int ToDo, int InProgress, int Blocked, int Done,
    IReadOnlyList<PersonProjectSummaryDto> Projects);

public record PersonProjectSummaryDto(
    int ProjectId, string ProjectTitle, string ProjectStatus,
    int AssignedTasks, int DoneTasks);

public record PersonTaskDto(
    int Id, string Title, string Status, string Priority,
    int ProjectId, string ProjectTitle,
    int? SprintId, string? SprintName, string? EpicTitle,
    string? DueDate);

public sealed class GetPersonProfileHandler(IAppDbContext db)
    : IRequestHandler<GetPersonProfileQuery, PersonProfileDto>
{
    public async Task<PersonProfileDto> Handle(GetPersonProfileQuery request, CancellationToken ct)
    {
        var person = await db.Persons.FindAsync([request.PersonId], ct)
            ?? throw new KeyNotFoundException($"Person {request.PersonId} not found.");

        var teams = await db.PersonTeamMemberships
            .Where(m => m.PersonId == request.PersonId)
            .Select(m => new PersonTeamDto(
                m.TeamId, m.Team.Name,
                m.Team.LeadPersonId == request.PersonId))
            .ToListAsync(ct);

        var myWorkItems = await db.WorkItems
            .Where(w => w.Assignees.Any(a => a.Id == request.PersonId))
            .Select(w => new
            {
                w.Id, w.Title, w.Status, w.Priority,
                w.ProjectId,
                ProjectTitle = w.Project != null ? w.Project.Title : string.Empty,
                ProjectStatus = w.Project != null ? w.Project.Status.ToString() : string.Empty,
                w.SprintId,
                SprintName = w.Sprint != null ? w.Sprint.Name : null,
                EpicTitle  = w.Epic  != null ? w.Epic.Title  : null,
                DueDate = w.DueDate != null ? w.DueDate.Value.ToString() : null,
            })
            .ToListAsync(ct);

        var byProject = myWorkItems
            .GroupBy(w => new { w.ProjectId, w.ProjectTitle, w.ProjectStatus })
            .Select(g => new PersonProjectSummaryDto(
                g.Key.ProjectId, g.Key.ProjectTitle, g.Key.ProjectStatus,
                g.Count(),
                g.Count(w => w.Status == WorkItemStatus.Done)))
            .OrderByDescending(p => p.AssignedTasks)
            .ToList();

        var workload = new PersonWorkloadDto(
            Total:      myWorkItems.Count,
            Backlog:    myWorkItems.Count(w => w.Status == WorkItemStatus.Backlog),
            ToDo:       myWorkItems.Count(w => w.Status == WorkItemStatus.ToDo),
            InProgress: myWorkItems.Count(w => w.Status == WorkItemStatus.InProgress),
            Blocked:    myWorkItems.Count(w => w.Status == WorkItemStatus.Blocked),
            Done:       myWorkItems.Count(w => w.Status == WorkItemStatus.Done),
            Projects:   byProject);

        var activeTasks = myWorkItems
            .Where(w => w.Status != WorkItemStatus.Done && w.Status != WorkItemStatus.Discarded && w.Status != WorkItemStatus.Backlog)
            .OrderBy(w => w.Priority == WorkItemPriority.Critical ? 0
                        : w.Priority == WorkItemPriority.High ? 1
                        : w.Priority == WorkItemPriority.Medium ? 2 : 3)
            .ThenBy(w => w.DueDate)
            .Select(w => new PersonTaskDto(
                w.Id, w.Title, w.Status.ToString(), w.Priority.ToString(),
                w.ProjectId, w.ProjectTitle, w.SprintId, w.SprintName, w.EpicTitle, w.DueDate))
            .ToList();

        return new PersonProfileDto(
            person.Id, person.Name, person.Email, person.Role.ToString(),
            teams, workload, activeTasks);
    }
}
