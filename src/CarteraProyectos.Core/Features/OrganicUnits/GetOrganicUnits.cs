using CarteraProyectos.Core.Common;
using CarteraProyectos.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.OrganicUnits;

public record GetOrganicUnitsQuery(string? Q = null, int Page = 1, int PageSize = 20) : IRequest<PagedResult<OrganicUnitDto>>;

public record OrganicUnitDto(int Id, string Name, string? Code);

public sealed class GetOrganicUnitsHandler(IAppDbContext db) : IRequestHandler<GetOrganicUnitsQuery, PagedResult<OrganicUnitDto>>
{
    public async Task<PagedResult<OrganicUnitDto>> Handle(GetOrganicUnitsQuery request, CancellationToken cancellationToken)
    {
        var pageSize = Math.Min(request.PageSize, 100);
        var page = Math.Max(request.Page, 1);

        var query = db.OrganicUnits.AsQueryable();
        if (!string.IsNullOrWhiteSpace(request.Q))
            query = query.Where(u => u.Name.Contains(request.Q) || (u.Code != null && u.Code.Contains(request.Q)));

        query = query.OrderBy(u => u.Name);
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new OrganicUnitDto(u.Id, u.Name, u.Code))
            .ToListAsync(cancellationToken);

        return new PagedResult<OrganicUnitDto>(items, total, page, pageSize);
    }
}
