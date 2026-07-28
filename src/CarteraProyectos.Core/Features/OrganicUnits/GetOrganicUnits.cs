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

        if (!string.IsNullOrWhiteSpace(request.Q))
        {
            // Filtro normalizado en memoria: insensible a mayúsculas/minúsculas y acentos.
            // Criterio: Name contiene q O Code contiene q (cada campo normalizado independientemente).
            var qNorm = TextSearchNormalizer.Normalize(request.Q);

            var all = await db.OrganicUnits
                .OrderBy(u => u.Name)
                .Select(u => new OrganicUnitDto(u.Id, u.Name, u.Code))
                .ToListAsync(cancellationToken);

            var filtered = all
                .Where(u =>
                    TextSearchNormalizer.Normalize(u.Name).Contains(qNorm, StringComparison.Ordinal) ||
                    (u.Code != null && TextSearchNormalizer.Normalize(u.Code).Contains(qNorm, StringComparison.Ordinal)))
                .ToList();

            var total = filtered.Count;
            var items = filtered
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new PagedResult<OrganicUnitDto>(items, total, page, pageSize);
        }
        else
        {
            // Sin búsqueda: orden + paginación en SQL (camino original, sin coste añadido).
            var query = db.OrganicUnits.OrderBy(u => u.Name);
            var total = await query.CountAsync(cancellationToken);
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new OrganicUnitDto(u.Id, u.Name, u.Code))
                .ToListAsync(cancellationToken);

            return new PagedResult<OrganicUnitDto>(items, total, page, pageSize);
        }
    }
}
