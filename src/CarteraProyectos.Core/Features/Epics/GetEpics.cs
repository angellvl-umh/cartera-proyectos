using CarteraProyectos.Core.Common;
using CarteraProyectos.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.Epics;

public record GetEpicsQuery(int ProjectId, int Page = 1, int PageSize = 20) : IRequest<PagedResult<EpicDto>>;

public record EpicDto(
    int Id, int ProjectId, string Title, string? Description,
    int Priority, int SortOrder, int WorkItemCount,
    int? EstimationHours, int? EstimationPoints);

public sealed class GetEpicsHandler(IAppDbContext db) : IRequestHandler<GetEpicsQuery, PagedResult<EpicDto>>
{
    public async Task<PagedResult<EpicDto>> Handle(GetEpicsQuery request, CancellationToken cancellationToken)
    {
        var pageSize = Math.Min(request.PageSize, 100);
        var page = Math.Max(request.Page, 1);

        var query = db.Epics.Where(e => e.ProjectId == request.ProjectId);
        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(e => e.SortOrder).ThenBy(e => e.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new EpicDto(e.Id, e.ProjectId, e.Title, e.Description, e.Priority, e.SortOrder,
                e.WorkItems.Count, e.EstimationHours, e.EstimationPoints))
            .ToListAsync(cancellationToken);

        return new PagedResult<EpicDto>(items, total, page, pageSize);
    }
}
