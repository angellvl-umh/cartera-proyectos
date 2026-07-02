using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.Teams;

public record GetTeamActivityQuery : IRequest<IReadOnlyList<TeamActivityDto>>;

public record TeamActivityDto(
    int TeamId,
    string TeamName,
    string? LeadName,
    IReadOnlyList<PersonActivityDto> Members);

public record PersonActivityDto(
    int PersonId,
    string Name,
    string Role,
    IReadOnlyList<ActiveTaskDto> ActiveTasks);

public record ActiveTaskDto(
    int WorkItemId,
    string Title,
    string Status,
    string Priority,
    string Type,
    int ProjectId,
    string ProjectTitle,
    string? SprintName,
    string? DueDate,
    bool IsHito);

public sealed class GetTeamActivityHandler(IAppDbContext db)
    : IRequestHandler<GetTeamActivityQuery, IReadOnlyList<TeamActivityDto>>
{
    private static readonly WorkItemStatus[] ActiveStatuses =
        [WorkItemStatus.InProgress, WorkItemStatus.Blocked];

    public async Task<IReadOnlyList<TeamActivityDto>> Handle(
        GetTeamActivityQuery request,
        CancellationToken cancellationToken)
    {
        // Una sola query: equipos con miembros
        var teams = await db.Teams
            .Include(t => t.Lead)
            .Include(t => t.Members).ThenInclude(m => m.Person)
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);

        // Una sola query plana para todas las tareas activas con asignados
        var activeTasks = await db.WorkItems
            .Where(w => ActiveStatuses.Contains(w.Status))
            .Select(w => new
            {
                w.Id,
                w.Title,
                w.Status,
                w.Priority,
                w.Type,
                w.ProjectId,
                ProjectTitle = w.Project != null ? w.Project.Title : string.Empty,
                SprintName = w.Sprint != null ? w.Sprint.Name : null,
                DueDate = w.DueDate != null ? w.DueDate.Value.ToString() : null,
                w.IsHito,
                AssigneeIds = w.Assignees.Select(a => a.Id).ToList(),
            })
            .ToListAsync(cancellationToken);

        // Agrupar en memoria: personId → lista de tareas
        var tasksByPerson = activeTasks
            .SelectMany(t => t.AssigneeIds.Select(aId => (AssigneeId: aId, Task: t)))
            .GroupBy(x => x.AssigneeId)
            .ToDictionary(
                g => g.Key,
                g => g
                    .OrderByDescending(x => x.Task.Status == WorkItemStatus.Blocked)
                    .ThenByDescending(x => (int)x.Task.Priority)
                    .Select(x => new ActiveTaskDto(
                        x.Task.Id,
                        x.Task.Title,
                        x.Task.Status.ToString(),
                        x.Task.Priority.ToString(),
                        x.Task.Type.ToString(),
                        x.Task.ProjectId,
                        x.Task.ProjectTitle,
                        x.Task.SprintName,
                        x.Task.DueDate,
                        x.Task.IsHito))
                    .ToList()
            );

        var result = new List<TeamActivityDto>();

        foreach (var team in teams)
        {
            var memberDtos = team.Members
                .Where(m => m.Person is not null && m.Person.IsActive)
                .Select(m =>
                {
                    var personTasks = tasksByPerson.TryGetValue(m.PersonId, out var tasks)
                        ? (IReadOnlyList<ActiveTaskDto>)tasks
                        : [];
                    return new PersonActivityDto(
                        m.PersonId,
                        m.Person!.Name,
                        m.Person!.Role.ToString(),
                        personTasks);
                })
                // Personas con tareas activas primero (más tareas = primero), luego disponibles
                .OrderByDescending(p => p.ActiveTasks.Count > 0)
                .ThenByDescending(p => p.ActiveTasks.Count)
                .ThenBy(p => p.Name)
                .ToList();

            result.Add(new TeamActivityDto(
                team.Id,
                team.Name,
                team.Lead?.Name,
                memberDtos));
        }

        return result;
    }
}
