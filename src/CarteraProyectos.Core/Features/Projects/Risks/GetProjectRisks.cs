using CarteraProyectos.Core.Common;
using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Features.Projects;
using MediatR;

namespace CarteraProyectos.Core.Features.Projects.Risks;

public record GetProjectRisksQuery(int ProjectId, int Page = 1, int PageSize = 20)
    : IRequest<PagedResult<ProjectRiskDto>>;

public record ProjectRiskDto(
    int Id,
    int ProjectId,
    string Description,
    string Probability,
    string Impact,
    string? MitigationPlan,
    string Status,
    int Severity,
    int CreatedById,
    string CreatedByName,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed class GetProjectRisksHandler(IProjectGovernanceService service)
    : IRequestHandler<GetProjectRisksQuery, PagedResult<ProjectRiskDto>>
{
    public Task<PagedResult<ProjectRiskDto>> Handle(GetProjectRisksQuery request, CancellationToken cancellationToken)
        => service.GetRisksAsync(request, cancellationToken);
}
