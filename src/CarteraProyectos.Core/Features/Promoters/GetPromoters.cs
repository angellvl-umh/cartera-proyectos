using CarteraProyectos.Core.Common;
using CarteraProyectos.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.Promoters;

public record GetPromotersQuery(int Page = 1, int PageSize = 20) : IRequest<PagedResult<PromoterDto>>;

public record PromoterDto(int Id, string Name);

public sealed class GetPromotersHandler(IAppDbContext db) : IRequestHandler<GetPromotersQuery, PagedResult<PromoterDto>>
{
    public async Task<PagedResult<PromoterDto>> Handle(GetPromotersQuery request, CancellationToken cancellationToken)
    {
        var pageSize = Math.Min(request.PageSize, 100);
        var page = Math.Max(request.Page, 1);

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
