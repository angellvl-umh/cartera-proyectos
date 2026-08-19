using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Features.Projects;
using MediatR;

namespace CarteraProyectos.Core.Features.Projects.Dependencies;

public record GetProjectDependenciesQuery(int ProjectId) : IRequest<ProjectDependenciesDto>;

public record ProjectDependenciesDto(
    IReadOnlyList<DependencyItemDto> DependsOn,
    IReadOnlyList<DependencyItemDto> Dependents);

public record DependencyItemDto(
    int DependencyId,
    int ProjectId,
    string ProjectTitle,
    string ProjectStatus,
    string? Description);

public sealed class GetProjectDependenciesHandler(IProjectGovernanceService service)
    : IRequestHandler<GetProjectDependenciesQuery, ProjectDependenciesDto>
{
    public Task<ProjectDependenciesDto> Handle(GetProjectDependenciesQuery request, CancellationToken cancellationToken)
        => service.GetDependenciesAsync(request, cancellationToken);
}
