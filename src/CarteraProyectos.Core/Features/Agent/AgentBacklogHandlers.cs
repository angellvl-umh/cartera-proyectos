using CarteraProyectos.Core.Common;
using CarteraProyectos.Core.Features.WorkItems;
using MediatR;

namespace CarteraProyectos.Core.Features.Agent;

// ─── Backlog Commands ─────────────────────────────────────────────────────────

public record AgentReorderBacklogCommand(
    int PersonId,
    int ProjectId,
    IReadOnlyList<int> OrderedIds) : IRequest, IAgentAuditable
{
    public int RequestingPersonId => PersonId;
}

public record AgentBulkAssignToSprintCommand(
    int PersonId,
    int ProjectId,
    IReadOnlyList<int> WorkItemIds,
    int? SprintId) : IRequest, IAgentAuditable
{
    public int RequestingPersonId => PersonId;
}

// ─── Backlog Handlers ─────────────────────────────────────────────────────────

public sealed class AgentReorderBacklogHandler(ISender sender)
    : IRequestHandler<AgentReorderBacklogCommand>
{
    public async Task Handle(AgentReorderBacklogCommand request, CancellationToken ct)
        => await sender.Send(
            new ReorderWorkItemsCommand(request.ProjectId, request.OrderedIds),
            ct);
}

public sealed class AgentBulkAssignToSprintHandler(ISender sender)
    : IRequestHandler<AgentBulkAssignToSprintCommand>
{
    public async Task Handle(AgentBulkAssignToSprintCommand request, CancellationToken ct)
        => await sender.Send(
            new BulkAssignWorkItemsToSprintCommand(
                request.ProjectId,
                request.WorkItemIds,
                request.SprintId),
            ct);
}
