using CarteraProyectos.Core.Common;
using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Features.Projects;
using CarteraProyectos.Core.Features.Projects.Dependencies;
using CarteraProyectos.Core.Features.Projects.Risks;
using MediatR;

namespace CarteraProyectos.Core.Features.Agent;

// ─── DTOs ────────────────────────────────────────────────────────────────────

public record AgentProjectDependenciesDto(
    IReadOnlyList<AgentDependencyItemDto> DependsOn,
    IReadOnlyList<AgentDependencyItemDto> Dependents);

public record AgentDependencyItemDto(
    int ProjectId, string ProjectTitle, string? Description);

// ─── Queries / Commands ───────────────────────────────────────────────────────

public record AgentTransitionProjectStatusCommand(
    int PersonId, int ProjectId, string NewStatus) : IRequest, IAgentAuditable
{
    public int RequestingPersonId => PersonId;
}

public record AgentGetProjectRisksQuery(int ProjectId) : IRequest<IReadOnlyList<ProjectRiskDto>>;

public record AgentAddProjectRiskCommand(
    int PersonId, int ProjectId, string Description,
    string Probability, string Impact, string? MitigationPlan) : IRequest<int>, IAgentAuditable
{
    public int RequestingPersonId => PersonId;
}

public record AgentUpdateProjectRiskCommand(
    int PersonId, int ProjectId, int RiskId,
    string Description, string Probability, string Impact,
    string? MitigationPlan, string Status) : IRequest, IAgentAuditable
{
    public int RequestingPersonId => PersonId;
}

public record AgentGetProjectDependenciesQuery(int ProjectId) : IRequest<AgentProjectDependenciesDto>;

public record AgentAddProjectDependencyCommand(
    int PersonId, int ProjectId, int DependsOnProjectId,
    string? Description) : IRequest<int>, IAgentAuditable
{
    public int RequestingPersonId => PersonId;
}

// ─── Handlers ────────────────────────────────────────────────────────────────

public sealed class AgentTransitionProjectStatusHandler(ISender sender)
    : IRequestHandler<AgentTransitionProjectStatusCommand>
{
    public async Task Handle(AgentTransitionProjectStatusCommand request, CancellationToken ct)
    {
        if (!Enum.TryParse<ProjectStatus>(request.NewStatus, out var status))
        {
            var validStatuses = string.Join(", ", 
                Enum.GetNames(typeof(ProjectStatus)));
            throw new InvalidOperationException(
                $"Estado '{request.NewStatus}' no válido. Estados válidos: {validStatuses}");
        }

        await sender.Send(new TransitionProjectStatusCommand(request.ProjectId, status, request.PersonId), ct);
    }
}

public sealed class AgentGetProjectRisksHandler(ISender sender)
    : IRequestHandler<AgentGetProjectRisksQuery, IReadOnlyList<ProjectRiskDto>>
{
    public async Task<IReadOnlyList<ProjectRiskDto>> Handle(AgentGetProjectRisksQuery request, CancellationToken ct)
    {
        var result = await sender.Send(new GetProjectRisksQuery(request.ProjectId, 1, 100), ct);
        return result.Items;
    }
}

public sealed class AgentAddProjectRiskHandler(ISender sender)
    : IRequestHandler<AgentAddProjectRiskCommand, int>
{
    public async Task<int> Handle(AgentAddProjectRiskCommand request, CancellationToken ct)
    {
        if (!Enum.TryParse<RiskLevel>(request.Probability, out var probability))
            throw new InvalidOperationException(
                "Probability no válido. Valores: Low, Medium, High.");
        
        if (!Enum.TryParse<RiskLevel>(request.Impact, out var impact))
            throw new InvalidOperationException(
                "Impact no válido. Valores: Low, Medium, High.");

        return await sender.Send(
            new CreateProjectRiskCommand(
                request.ProjectId, request.PersonId, request.Description,
                probability, impact, request.MitigationPlan), ct);
    }
}

public sealed class AgentUpdateProjectRiskHandler(ISender sender)
    : IRequestHandler<AgentUpdateProjectRiskCommand>
{
    public async Task Handle(AgentUpdateProjectRiskCommand request, CancellationToken ct)
    {
        if (!Enum.TryParse<RiskLevel>(request.Probability, out var probability))
            throw new InvalidOperationException(
                "Probability no válido. Valores: Low, Medium, High.");
        
        if (!Enum.TryParse<RiskLevel>(request.Impact, out var impact))
            throw new InvalidOperationException(
                "Impact no válido. Valores: Low, Medium, High.");

        if (!Enum.TryParse<RiskStatus>(request.Status, out var status))
            throw new InvalidOperationException(
                "Status no válido. Valores: Open, Mitigated, Closed.");

        await sender.Send(
            new UpdateProjectRiskCommand(
                request.ProjectId, request.RiskId, request.PersonId,
                request.Description, probability, impact,
                request.MitigationPlan, status), ct);
    }
}

public sealed class AgentGetProjectDependenciesHandler(ISender sender)
    : IRequestHandler<AgentGetProjectDependenciesQuery, AgentProjectDependenciesDto>
{
    public async Task<AgentProjectDependenciesDto> Handle(AgentGetProjectDependenciesQuery request, CancellationToken ct)
    {
        var result = await sender.Send(new GetProjectDependenciesQuery(request.ProjectId), ct);
        
        return new AgentProjectDependenciesDto(
            result.DependsOn.Select(d => new AgentDependencyItemDto(d.ProjectId, d.ProjectTitle, d.Description)).ToList(),
            result.Dependents.Select(d => new AgentDependencyItemDto(d.ProjectId, d.ProjectTitle, d.Description)).ToList());
    }
}

public sealed class AgentAddProjectDependencyHandler(ISender sender)
    : IRequestHandler<AgentAddProjectDependencyCommand, int>
{
    public async Task<int> Handle(AgentAddProjectDependencyCommand request, CancellationToken ct)
    {
        return await sender.Send(
            new CreateProjectDependencyCommand(
                request.ProjectId, request.DependsOnProjectId,
                request.Description, request.PersonId), ct);
    }
}
