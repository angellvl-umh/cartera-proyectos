using CarteraProyectos.Core.Common;
using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Features.Projects;
using CarteraProyectos.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.Agent;

// ─── Commands ────────────────────────────────────────────────────────────────

public record AgentCreateProjectCommand(
    int PersonId, string Title, string? Description, string? RequestingUnit,
    string Complexity, int? PortfolioYear, DateOnly? StartDate, DateOnly? EndDate,
    int? BeneficiaryCount, int? PromoterId, int? OrganicUnitId, int? GroupPriority,
    DateOnly? DesiredDeploymentDate, string? SpecificationsUrl,
    string? EpicUrl, decimal? EstimatedBudget, int? BusinessValue) : IRequest<int>, IAgentAuditable
{
    public int RequestingPersonId => PersonId;
}

public record AgentUpdateProjectCommand(
    int PersonId, int ProjectId, string? Title, string? Description, string? RequestingUnit,
    string? Complexity, int? PortfolioYear, DateOnly? StartDate, DateOnly? EndDate,
    int? BeneficiaryCount, int? PromoterId, int? OrganicUnitId, int? GroupPriority,
    DateOnly? DesiredDeploymentDate, string? SpecificationsUrl,
    string? EpicUrl, decimal? EstimatedBudget, int? BusinessValue) : IRequest, IAgentAuditable
{
    public int RequestingPersonId => PersonId;
}

// ─── Handlers ────────────────────────────────────────────────────────────────

public sealed class AgentCreateProjectHandler(ISender sender)
    : IRequestHandler<AgentCreateProjectCommand, int>
{
    public async Task<int> Handle(AgentCreateProjectCommand request, CancellationToken ct)
    {
        // Parsear Complexity (obligatorio)
        if (!Enum.TryParse<ProjectComplexity>(request.Complexity, out var complexity))
            throw new InvalidOperationException("Complejidad no válida. Valores aceptados: VerySmall, Small, Medium, Large, VeryLarge.");

        // Delegar al command core
        var result = await sender.Send(
            new CreateProjectCommand(
                request.Title, request.Description, request.RequestingUnit, complexity,
                request.PortfolioYear, request.StartDate, request.EndDate,
                RequestingPersonId: request.PersonId,
                PreviousReferenceId: null,
                BeneficiaryCount: request.BeneficiaryCount,
                PromoterId: request.PromoterId,
                OrganicUnitId: request.OrganicUnitId,
                UorOrder: null,
                GroupPriority: request.GroupPriority,
                DesiredDeploymentDate: request.DesiredDeploymentDate,
                SpecificationsUrl: request.SpecificationsUrl,
                EpicUrl: request.EpicUrl,
                EstimatedBudget: request.EstimatedBudget,
                BusinessValue: request.BusinessValue,
                TagIds: null),
            ct);

        return result;
    }
}

public sealed class AgentUpdateProjectHandler(IAppDbContext db, ISender sender)
    : IRequestHandler<AgentUpdateProjectCommand>
{
    public async Task Handle(AgentUpdateProjectCommand request, CancellationToken ct)
    {
        // Cargar el proyecto actual para obtener los valores sin cambios
        var project = await db.Projects
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.ProjectId, ct)
            ?? throw new KeyNotFoundException($"Proyecto con Id {request.ProjectId} no encontrado.");

        // Determinar valores finales (usar actuales si request es null)
        var title = request.Title ?? project.Title;
        var description = request.Description ?? project.Description;
        var requestingUnit = request.RequestingUnit ?? project.RequestingUnit;
        var portfolioYear = request.PortfolioYear ?? project.PortfolioYear;
        var startDate = request.StartDate ?? project.StartDate;
        var endDate = request.EndDate ?? project.EndDate;
        var beneficiaryCount = request.BeneficiaryCount ?? project.BeneficiaryCount;
        var promoterId = request.PromoterId ?? project.PromoterId;
        var organicUnitId = request.OrganicUnitId ?? project.OrganicUnitId;
        var groupPriority = request.GroupPriority ?? project.GroupPriority;
        var desiredDeploymentDate = request.DesiredDeploymentDate ?? project.DesiredDeploymentDate;
        var specificationsUrl = request.SpecificationsUrl ?? project.SpecificationsUrl;
        var epicUrl = request.EpicUrl ?? project.EpicUrl;
        var estimatedBudget = request.EstimatedBudget ?? project.EstimatedBudget;
        var businessValue = request.BusinessValue ?? project.BusinessValue;

        // Parsear Complexity (si viene especificada en el request)
        ProjectComplexity complexity;
        if (request.Complexity is null)
        {
            complexity = project.Complexity;
        }
        else
        {
            if (!Enum.TryParse<ProjectComplexity>(request.Complexity, out var parsed))
                throw new InvalidOperationException("Complejidad no válida. Valores aceptados: VerySmall, Small, Medium, Large, VeryLarge.");
            complexity = parsed;
        }

        // Delegar al command core con todos los valores finales
        await sender.Send(
            new UpdateProjectCommand(
                request.ProjectId,
                title, description, requestingUnit, complexity,
                portfolioYear, startDate, endDate,
                RequestingPersonId: request.PersonId,
                PreviousReferenceId: project.PreviousReferenceId,
                BeneficiaryCount: beneficiaryCount,
                PromoterId: promoterId,
                OrganicUnitId: organicUnitId,
                UorOrder: project.UorOrder,
                GroupPriority: groupPriority,
                DesiredDeploymentDate: desiredDeploymentDate,
                SpecificationsUrl: specificationsUrl,
                EpicUrl: epicUrl,
                EstimatedBudget: estimatedBudget,
                BusinessValue: businessValue,
                TagIds: null),
            ct);
    }
}
