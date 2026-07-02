using CarteraProyectos.Core.Common;
using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

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

public sealed class GetProjectRisksHandler(IAppDbContext db)
    : IRequestHandler<GetProjectRisksQuery, PagedResult<ProjectRiskDto>>
{
    public async Task<PagedResult<ProjectRiskDto>> Handle(GetProjectRisksQuery request, CancellationToken cancellationToken)
    {
        var pageSize = Math.Min(request.PageSize, 100);
        var page = Math.Max(request.Page, 1);

        // Ordenar: Open primero, luego Mitigated, luego Closed; dentro de cada grupo por Severity desc
        var query = db.ProjectRisks
            .Include(r => r.CreatedBy)
            .Where(r => r.ProjectId == request.ProjectId)
            .OrderBy(r => r.Status == RiskStatus.Open ? 0 : r.Status == RiskStatus.Mitigated ? 1 : 2)
            .ThenByDescending(r => ((int)r.Probability + 1) * ((int)r.Impact + 1));

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ProjectRiskDto>(
            items.Select(r => new ProjectRiskDto(
                r.Id, r.ProjectId, r.Description,
                r.Probability.ToString(), r.Impact.ToString(),
                r.MitigationPlan, r.Status.ToString(),
                r.Severity,
                r.CreatedById, r.CreatedBy!.Name,
                r.CreatedAt, r.UpdatedAt)).ToList(),
            total, page, pageSize);
    }
}
