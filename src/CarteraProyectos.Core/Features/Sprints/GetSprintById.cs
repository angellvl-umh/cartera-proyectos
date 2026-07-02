using CarteraProyectos.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.Sprints;

public record GetSprintByIdQuery(int ProjectId, int SprintId) : IRequest<SprintDto>;

public sealed class GetSprintByIdHandler(IAppDbContext db) : IRequestHandler<GetSprintByIdQuery, SprintDto>
{
    public async Task<SprintDto> Handle(GetSprintByIdQuery request, CancellationToken cancellationToken)
    {
        var sprint = await db.Sprints
            .Where(s => s.Id == request.SprintId && s.ProjectId == request.ProjectId)
            .Select(s => new SprintDto(
                s.Id, s.ProjectId, s.Name, s.Goal,
                s.StartDate, s.EndDate,
                s.Status.ToString(), s.Capacity,
                s.WorkItems.Count,
                s.WorkItems.Sum(w => w.EstimationHours ?? 0),
                s.WorkItems.Sum(w => w.EstimationPoints ?? 0),
                s.CommittedPoints, s.DeliveredPoints))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException($"Sprint {request.SprintId} not found in project {request.ProjectId}.");

        return sprint;
    }
}
