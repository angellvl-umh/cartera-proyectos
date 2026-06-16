using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.Reports;

public record GetCapacityQuery : IRequest<IReadOnlyList<TeamCapacityDto>>;

public record TeamCapacityDto(
    int TeamId, string TeamName, string? LeadName,
    int ActiveProjectCount,
    IReadOnlyList<MemberCapacityDto> Members);

public record MemberCapacityDto(
    int PersonId, string Name, string Role,
    int ActiveTasks,   // InProgress + Blocked
    int PendingTasks,  // ToDo + Backlog
    int DoneTasks,
    string LoadLevel); // Green | Yellow | Red

public sealed class GetCapacityHandler(IAppDbContext db)
    : IRequestHandler<GetCapacityQuery, IReadOnlyList<TeamCapacityDto>>
{
    public async Task<IReadOnlyList<TeamCapacityDto>> Handle(GetCapacityQuery _, CancellationToken ct)
    {
        var teams = await db.Teams
            .Include(t => t.Lead)
            .Include(t => t.Members).ThenInclude(m => m.Person)
            .Include(t => t.Projects).ThenInclude(pta => pta.Project)
            .OrderBy(t => t.Name)
            .ToListAsync(ct);

        var activeProjectStatuses = new[]
        {
            ProjectStatus.PlanningWithClient,
            ProjectStatus.PlanningSprint,
            ProjectStatus.InSprint,
            ProjectStatus.DevelopmentOutsideSprint,
            ProjectStatus.InTesting,
        };

        var result = new List<TeamCapacityDto>();

        foreach (var team in teams)
        {
            var activeProjectCount = team.Projects
                .Count(pta => pta.Project != null && activeProjectStatuses.Contains(pta.Project.Status));

            var members = new List<MemberCapacityDto>();

            foreach (var membership in team.Members)
            {
                var person = membership.Person;
                if (person is null) continue;

                var activeTasks = await db.WorkItems.CountAsync(w =>
                    w.Assignees.Any(a => a.Id == person.Id) &&
                    (w.Status == WorkItemStatus.InProgress || w.Status == WorkItemStatus.Blocked), ct);

                var pendingTasks = await db.WorkItems.CountAsync(w =>
                    w.Assignees.Any(a => a.Id == person.Id) &&
                    (w.Status == WorkItemStatus.ToDo || w.Status == WorkItemStatus.Backlog), ct);

                var doneTasks = await db.WorkItems.CountAsync(w =>
                    w.Assignees.Any(a => a.Id == person.Id) &&
                    w.Status == WorkItemStatus.Done, ct);

                var loadLevel = activeTasks <= 3 ? "Green" : activeTasks <= 6 ? "Yellow" : "Red";

                members.Add(new MemberCapacityDto(
                    person.Id, person.Name, person.Role.ToString(),
                    activeTasks, pendingTasks, doneTasks, loadLevel));
            }

            result.Add(new TeamCapacityDto(
                team.Id, team.Name, team.Lead?.Name,
                activeProjectCount,
                members.OrderByDescending(m => m.ActiveTasks).ToList()));
        }

        return result;
    }
}
