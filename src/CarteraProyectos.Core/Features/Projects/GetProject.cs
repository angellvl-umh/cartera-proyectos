using CarteraProyectos.Core.Features.Tags;
using CarteraProyectos.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.Projects;

public record GetProjectQuery(int Id) : IRequest<ProjectDetailDto?>;

public record ProjectTeamDto(int TeamId, string TeamName, bool IsPrimary);

public record ProjectDetailDto(
    int Id,
    string Title,
    string? Description,
    string? RequestingUnit,
    string Complexity,
    string Status,
    int? PortfolioYear,
    DateOnly? StartDate,
    DateOnly? EndDate,
    List<ProjectTeamDto> Teams,
    // Extended fields
    int? PreviousReferenceId,
    int? BeneficiaryCount,
    int? PromoterId,
    string? PromoterName,
    int? OrganicUnitId,
    string? OrganicUnitName,
    int? UorOrder,
    int? GroupPriority,
    string? SiptGroup,
    DateOnly? DesiredDeploymentDate,
    string? SpecificationsUrl,
    string? EpicUrl,
    decimal? EstimatedBudget,
    List<TagDto> Tags);

public sealed class GetProjectHandler(IAppDbContext db) : IRequestHandler<GetProjectQuery, ProjectDetailDto?>
{
    public async Task<ProjectDetailDto?> Handle(GetProjectQuery request, CancellationToken cancellationToken)
    {
        var project = await db.Projects
            .Include(p => p.Teams).ThenInclude(a => a.Team)
            .Include(p => p.Promoter)
            .Include(p => p.OrganicUnit)
            .Include(p => p.Tags)
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (project is null) return null;

        return new ProjectDetailDto(
            project.Id,
            project.Title,
            project.Description,
            project.RequestingUnit,
            project.Complexity.ToString(),
            project.Status.ToString(),
            project.PortfolioYear,
            project.StartDate,
            project.EndDate,
            project.Teams.Select(a => new ProjectTeamDto(a.TeamId, a.Team!.Name, a.IsPrimary)).ToList(),
            project.PreviousReferenceId,
            project.BeneficiaryCount,
            project.PromoterId,
            project.Promoter?.Name,
            project.OrganicUnitId,
            project.OrganicUnit?.Name,
            project.UorOrder,
            project.GroupPriority,
            project.SiptGroup?.ToString(),
            project.DesiredDeploymentDate,
            project.SpecificationsUrl,
            project.EpicUrl,
            project.EstimatedBudget,
            project.Tags.Select(t => new TagDto(t.Id, t.Name, t.Color)).ToList());
    }
}
