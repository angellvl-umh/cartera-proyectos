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
        // Cargar equipos con sus relaciones (teams, lead, members, projects)
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

        // Query única: agregación de conteos por (PersonId, WorkItemStatus)
        var taskCountsByPerson = await db.WorkItems
            .SelectMany(w => w.Assignees.Select(a => new { PersonId = a.Id, w.Status }))
            .GroupBy(x => new { x.PersonId, x.Status })
            .Select(g => new { g.Key.PersonId, g.Key.Status, Count = g.Count() })
            .ToListAsync(ct);

        // Construir diccionario para acceso O(1): personId → { status → count }
        var countsDict = taskCountsByPerson
            .GroupBy(x => x.PersonId)
            .ToDictionary(
                grp => grp.Key,
                grp => grp.ToDictionary(x => x.Status, x => x.Count));

        var result = new List<TeamCapacityDto>();

        foreach (var team in teams)
        {
            var activeProjectCount = team.Projects
                .Count(pta => pta.Project != null && activeProjectStatuses.Contains(pta.Project.Status));

            var members = new List<MemberCapacityDto>();

            foreach (var membership in team.Members)
            {
                var person = membership.Person;
                if (person is null || !person.IsActive) continue;

                // Obtener conteos del diccionario; por defecto 0 si no hay tareas
                var statusCounts = countsDict.TryGetValue(person.Id, out var counts) ? counts : new Dictionary<WorkItemStatus, int>();

                var activeTasks = (statusCounts.TryGetValue(WorkItemStatus.InProgress, out var inProg) ? inProg : 0) +
                                  (statusCounts.TryGetValue(WorkItemStatus.Blocked, out var blocked) ? blocked : 0);

                var pendingTasks = (statusCounts.TryGetValue(WorkItemStatus.ToDo, out var todo) ? todo : 0) +
                                   (statusCounts.TryGetValue(WorkItemStatus.Backlog, out var backlog) ? backlog : 0);

                var doneTasks = statusCounts.TryGetValue(WorkItemStatus.Done, out var done) ? done : 0;

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
