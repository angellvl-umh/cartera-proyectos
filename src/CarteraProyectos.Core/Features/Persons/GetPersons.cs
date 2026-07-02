using CarteraProyectos.Core.Common;
using CarteraProyectos.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.Persons;

public record GetPersonsQuery(int Page = 1, int PageSize = 20, bool IncludeInactive = false) : IRequest<PagedResult<PersonListDto>>;

public record PersonListDto(int Id, string Name, string Email, string Role, bool IsActive, bool HasLoggedIn);

public sealed class GetPersonsHandler(IAppDbContext db) : IRequestHandler<GetPersonsQuery, PagedResult<PersonListDto>>
{
    public async Task<PagedResult<PersonListDto>> Handle(GetPersonsQuery request, CancellationToken cancellationToken)
    {
        var pageSize = Math.Min(request.PageSize, 100);
        var page = Math.Max(request.Page, 1);

        var baseQuery = db.Persons
            .Where(p => request.IncludeInactive || p.IsActive)
            .OrderBy(p => p.Name);

        var total = await baseQuery.CountAsync(cancellationToken);

        var items = await baseQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PersonListDto(
                p.Id, p.Name, p.Email, p.Role.ToString(), p.IsActive, p.SubjectId != null))
            .ToListAsync(cancellationToken);

        return new PagedResult<PersonListDto>(items, total, page, pageSize);
    }
}
