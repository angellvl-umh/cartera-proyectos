using MediatR;

namespace CarteraProyectos.Core.Features.WorkItems;

public record ReorderWorkItemsCommand(int ProjectId, IReadOnlyList<int> OrderedIds) : IRequest;

public sealed class ReorderWorkItemsHandler(IWorkItemLifecycleService service)
    : IRequestHandler<ReorderWorkItemsCommand>
{
    public Task Handle(ReorderWorkItemsCommand request, CancellationToken cancellationToken)
        => service.ReorderAsync(request.ProjectId, request.OrderedIds, cancellationToken);
}
