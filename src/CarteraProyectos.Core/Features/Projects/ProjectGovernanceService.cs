using CarteraProyectos.Core.Common;
using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Features.Projects.Dependencies;
using CarteraProyectos.Core.Features.Projects.Risks;
using CarteraProyectos.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.Projects;

public interface IProjectGovernanceService
{
    Task<PagedResult<ProjectRiskDto>> GetRisksAsync(GetProjectRisksQuery query, CancellationToken ct);
    Task<int> AddRiskAsync(CreateProjectRiskCommand command, CancellationToken ct);
    Task UpdateRiskAsync(UpdateProjectRiskCommand command, CancellationToken ct);
    Task<ProjectDependenciesDto> GetDependenciesAsync(GetProjectDependenciesQuery query, CancellationToken ct);
    Task<int> AddDependencyAsync(CreateProjectDependencyCommand command, CancellationToken ct);
}

public sealed class ProjectGovernanceService(IAppDbContext db) : IProjectGovernanceService
{
    public async Task<PagedResult<ProjectRiskDto>> GetRisksAsync(GetProjectRisksQuery request, CancellationToken ct)
    {
        var pageSize = Math.Min(request.PageSize, 100);
        var page = Math.Max(request.Page, 1);

        // Ordenar: Open primero, luego Mitigated, luego Closed; dentro de cada grupo por Severity desc
        var query = db.ProjectRisks
            .Include(r => r.CreatedBy)
            .Where(r => r.ProjectId == request.ProjectId)
            .OrderBy(r => r.Status == RiskStatus.Open ? 0 : r.Status == RiskStatus.Mitigated ? 1 : 2)
            // Los enums se almacenan como string en BD: usar CASE traducible en vez de casts a int
            .ThenByDescending(r =>
                (r.Probability == RiskLevel.High ? 3 : r.Probability == RiskLevel.Medium ? 2 : 1) *
                (r.Impact == RiskLevel.High ? 3 : r.Impact == RiskLevel.Medium ? 2 : 1));

        var total = await query.CountAsync(ct);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

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

    public async Task<int> AddRiskAsync(CreateProjectRiskCommand request, CancellationToken ct)
    {
        if (!await db.Projects.AnyAsync(p => p.Id == request.ProjectId, ct))
            throw new KeyNotFoundException($"Proyecto con Id {request.ProjectId} no encontrado.");

        var requester = await db.Persons.FindAsync([request.RequestingPersonId], ct)
            ?? throw new KeyNotFoundException($"Persona con Id {request.RequestingPersonId} no encontrada.");

        await ProjectAuthorization.EnsureCanManageProjectAsync(db, request.ProjectId, requester, ct);

        var risk = ProjectRisk.Create(
            request.ProjectId, request.Description,
            request.Probability, request.Impact,
            request.MitigationPlan, request.RequestingPersonId);

        db.ProjectRisks.Add(risk);
        await db.SaveChangesAsync(ct);
        return risk.Id;
    }

    public async Task UpdateRiskAsync(UpdateProjectRiskCommand request, CancellationToken ct)
    {
        var risk = await db.ProjectRisks
            .FirstOrDefaultAsync(r => r.Id == request.RiskId && r.ProjectId == request.ProjectId, ct)
            ?? throw new KeyNotFoundException($"Riesgo con Id {request.RiskId} no encontrado en el proyecto {request.ProjectId}.");

        var requester = await db.Persons.FindAsync([request.RequestingPersonId], ct)
            ?? throw new KeyNotFoundException($"Persona con Id {request.RequestingPersonId} no encontrada.");

        await ProjectAuthorization.EnsureCanManageProjectAsync(db, request.ProjectId, requester, ct);

        risk.Update(request.Description, request.Probability, request.Impact,
            request.MitigationPlan, request.Status);

        await db.SaveChangesAsync(ct);
    }

    public async Task<ProjectDependenciesDto> GetDependenciesAsync(GetProjectDependenciesQuery request, CancellationToken ct)
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
            .ToListAsync(ct);

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
            .ToListAsync(ct);

        return new ProjectDependenciesDto(dependsOn, dependents);
    }

    public async Task<int> AddDependencyAsync(CreateProjectDependencyCommand request, CancellationToken ct)
    {
        // Validar que ambos proyectos existen
        if (!await db.Projects.AnyAsync(p => p.Id == request.ProjectId, ct))
            throw new KeyNotFoundException($"Proyecto con Id {request.ProjectId} no encontrado.");

        if (!await db.Projects.AnyAsync(p => p.Id == request.DependsOnProjectId, ct))
            throw new KeyNotFoundException($"Proyecto con Id {request.DependsOnProjectId} no encontrado.");

        // Validar permisos
        var requester = await db.Persons.FindAsync([request.RequestingPersonId], ct)
            ?? throw new KeyNotFoundException($"Persona con Id {request.RequestingPersonId} no encontrada.");

        await ProjectAuthorization.EnsureCanManageProjectAsync(db, request.ProjectId, requester, ct);

        // Validar que no exista duplicado
        var duplicate = await db.ProjectDependencies.AnyAsync(
            d => d.ProjectId == request.ProjectId && d.DependsOnProjectId == request.DependsOnProjectId,
            ct);
        if (duplicate)
            throw new InvalidOperationException(
                $"Ya existe una dependencia entre el proyecto {request.ProjectId} y el proyecto {request.DependsOnProjectId}.");

        // Validar que no genere ciclo directo (si B ya depende de A, no permitir A→B)
        var wouldCreateCycle = await db.ProjectDependencies.AnyAsync(
            d => d.ProjectId == request.DependsOnProjectId && d.DependsOnProjectId == request.ProjectId,
            ct);
        if (wouldCreateCycle)
            throw new InvalidOperationException(
                $"No se puede crear la dependencia: el proyecto {request.DependsOnProjectId} ya depende del proyecto {request.ProjectId}, " +
                "lo que generaría un ciclo directo.");

        var dependency = ProjectDependency.Create(request.ProjectId, request.DependsOnProjectId, request.Description);
        db.ProjectDependencies.Add(dependency);
        await db.SaveChangesAsync(ct);
        return dependency.Id;
    }
}
