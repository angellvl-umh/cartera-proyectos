using CarteraProyectos.Core.Common;
using CarteraProyectos.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.Sprints;

public record GetSprintsQuery(int ProjectId, int Page = 1, int PageSize = 20) : IRequest<PagedResult<SprintDto>>;

public record SprintDto(
    int Id, int ProjectId, string Name, string? Goal,
    DateOnly? StartDate, DateOnly? EndDate,
    string Status, int? Capacity,
    int WorkItemCount, int TotalEstimationHours, int TotalEstimationPoints);

public sealed class GetSprintsHandler(IAppDbContext db) : IRequestHandler<GetSprintsQuery, PagedResult<SprintDto>>
{
    public async Task<PagedResult<SprintDto>> Handle(GetSprintsQuery request, CancellationToken cancellationToken)
    {
        var pageSize = Math.Min(request.PageSize, 100);
        var page = Math.Max(request.Page, 1);

        var query = db.Sprints.Where(s => s.ProjectId == request.ProjectId);
        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(s => s.StartDate).ThenByDescending(s => s.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new SprintDto(
                s.Id, s.ProjectId, s.Name, s.Goal,
                s.StartDate, s.EndDate,
                s.Status.ToString(), s.Capacity,
                s.WorkItems.Count,
                s.WorkItems.Sum(w => w.EstimationHours ?? 0),
                s.WorkItems.Sum(w => w.EstimationPoints ?? 0)))
            .ToListAsync(cancellationToken);

        return new PagedResult<SprintDto>(items, total, page, pageSize);
    }
}
