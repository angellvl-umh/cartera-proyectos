using CarteraProyectos.Core.Common;
using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.Teams;

public record GetTeamsQuery(int Page = 1, int PageSize = 20) : IRequest<PagedResult<TeamDto>>;

public record TeamDto(
    int Id,
    string Name,
    string? Description,
    int? LeadPersonId,
    string? LeadName,
    int MemberCount);

public sealed class GetTeamsHandler(IAppDbContext db) : IRequestHandler<GetTeamsQuery, PagedResult<TeamDto>>
{
    public async Task<PagedResult<TeamDto>> Handle(GetTeamsQuery request, CancellationToken cancellationToken)
    {
        var pageSize = Math.Min(request.PageSize, 100);
        var page = Math.Max(request.Page, 1);

        var baseQuery = db.Teams
            .Include(t => t.Lead)
            .Include(t => t.Members)
            .OrderBy(t => t.Name);

        var total = await baseQuery.CountAsync(cancellationToken);

        var items = await baseQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TeamDto(
                t.Id,
                t.Name,
                t.Description,
                t.LeadPersonId,
                t.Lead != null ? t.Lead.Name : null,
                t.Members.Count))
            .ToListAsync(cancellationToken);

        return new PagedResult<TeamDto>(items, total, page, pageSize);
    }
}
