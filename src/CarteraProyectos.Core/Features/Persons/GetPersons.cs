using CarteraProyectos.Core.Common;
using MediatR;

namespace CarteraProyectos.Core.Features.Persons;

public record GetPersonsQuery(int Page = 1, int PageSize = 20, bool IncludeInactive = false) : IRequest<PagedResult<PersonListDto>>;

public record PersonListDto(int Id, string Name, string Email, string Role, bool IsActive, bool HasLoggedIn);

public sealed class GetPersonsHandler(IPersonManagementService service)
    : IRequestHandler<GetPersonsQuery, PagedResult<PersonListDto>>
{
    public Task<PagedResult<PersonListDto>> Handle(GetPersonsQuery request, CancellationToken cancellationToken)
        => service.GetListAsync(request, cancellationToken);
}
