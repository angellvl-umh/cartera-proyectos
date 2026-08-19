using MediatR;

namespace CarteraProyectos.Core.Features.WorkItems;

public record BulkAssignWorkItemsToSprintCommand(
    int ProjectId,
    IReadOnlyList<int> WorkItemIds,
    int? SprintId) : IRequest;

public sealed class BulkAssignWorkItemsToSprintHandler(IWorkItemLifecycleService service)
    : IRequestHandler<BulkAssignWorkItemsToSprintCommand>
{
    public Task Handle(BulkAssignWorkItemsToSprintCommand request, CancellationToken cancellationToken)
        => service.BulkAssignToSprintAsync(request.ProjectId, request.WorkItemIds, request.SprintId, cancellationToken);
}
