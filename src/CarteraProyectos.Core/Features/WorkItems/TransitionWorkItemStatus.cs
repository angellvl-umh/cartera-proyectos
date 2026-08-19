using CarteraProyectos.Core.Domain;
using MediatR;

namespace CarteraProyectos.Core.Features.WorkItems;

public record TransitionWorkItemStatusCommand(int Id, WorkItemStatus NewStatus, int RequestingPersonId = 0) : IRequest;

public sealed class TransitionWorkItemStatusHandler(IWorkItemLifecycleService service)
    : IRequestHandler<TransitionWorkItemStatusCommand>
{
    public Task Handle(TransitionWorkItemStatusCommand request, CancellationToken cancellationToken)
        => service.TransitionStatusAsync(request.Id, request.NewStatus, request.RequestingPersonId, cancellationToken);
}
