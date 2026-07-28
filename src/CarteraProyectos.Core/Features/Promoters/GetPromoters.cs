using CarteraProyectos.Core.Common;
using CarteraProyectos.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.Promoters;

public record GetPromotersQuery(string? Q = null, int Page = 1, int PageSize = 20) : IRequest<PagedResult<PromoterDto>>;

public record PromoterDto(int Id, string Name);

public sealed class GetPromotersHandler(IAppDbContext db) : IRequestHandler<GetPromotersQuery, PagedResult<PromoterDto>>
{
    public async Task<PagedResult<PromoterDto>> Handle(GetPromotersQuery request, CancellationToken cancellationToken)
    {
        var pageSize = Math.Min(request.PageSize, 100);
        var page = Math.Max(request.Page, 1);

        if (!string.IsNullOrWhiteSpace(request.Q))
        {
            // Filtro normalizado en memoria: insensible a mayúsculas/minúsculas y acentos.
            // Se carga todo el catálogo (pequeño por diseño) y se filtra en memoria con
            // TextSearchNormalizer para que "promocion" encuentre "Promoción".
            var qNorm = TextSearchNormalizer.Normalize(request.Q);

            var all = await db.Promoters
                .OrderBy(p => p.Name)
                .Select(p => new PromoterDto(p.Id, p.Name))
                .ToListAsync(cancellationToken);

            var filtered = all
                .Where(p => TextSearchNormalizer.Normalize(p.Name)
                    .Contains(qNorm, StringComparison.Ordinal))
                .ToList();

            var total = filtered.Count;
            var items = filtered
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new PagedResult<PromoterDto>(items, total, page, pageSize);
        }
        else
        {
            // Sin búsqueda: orden + paginación en SQL (camino original, sin coste añadido).
            var query = db.Promoters.OrderBy(p => p.Name);
            var total = await query.CountAsync(cancellationToken);
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new PromoterDto(p.Id, p.Name))
                .ToListAsync(cancellationToken);

            return new PagedResult<PromoterDto>(items, total, page, pageSize);
        }
    }
}
