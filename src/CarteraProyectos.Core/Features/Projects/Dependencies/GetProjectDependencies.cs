using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

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

public sealed class GetProjectDependenciesHandler(IAppDbContext db)
    : IRequestHandler<GetProjectDependenciesQuery, ProjectDependenciesDto>
{
    public async Task<ProjectDependenciesDto> Handle(GetProjectDependenciesQuery request, CancellationToken cancellationToken)
    {
        // Proyectos de los que depende (ProjectId = request.ProjectId → DependsOnProject)
        var dependsOn = await db.ProjectDependencies
            .Include(d => d.DependsOnProject)
            .Where(d => d.ProjectId == request.ProjectId)
            .OrderBy(d => d.DependsOnProject!.Title)
            .Select(d => new DependencyItemDto(
                d.Id,
                d.DependsOnProjectId,
                d.DependsOnProject!.Title,
                d.DependsOnProject.Status.ToString(),
                d.Description))
            .ToListAsync(cancellationToken);

        // Proyectos que dependen de éste (DependsOnProjectId = request.ProjectId → Project)
        var dependents = await db.ProjectDependencies
            .Include(d => d.Project)
            .Where(d => d.DependsOnProjectId == request.ProjectId)
            .OrderBy(d => d.Project!.Title)
            .Select(d => new DependencyItemDto(
                d.Id,
                d.ProjectId,
                d.Project!.Title,
                d.Project.Status.ToString(),
                d.Description))
            .ToListAsync(cancellationToken);

        return new ProjectDependenciesDto(dependsOn, dependents);
    }
}
