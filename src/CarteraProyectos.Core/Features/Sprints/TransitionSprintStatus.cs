using CarteraProyectos.Core.Domain;
using MediatR;

namespace CarteraProyectos.Core.Features.Sprints;

public enum CarryOverTarget { Backlog, Sprint }

public record TransitionSprintStatusCommand(
    int Id,
    SprintStatus NewStatus,
    int RequestingPersonId = 0,
    CarryOverTarget? CarryOver = null,
    int? TargetSprintId = null) : IRequest;

public sealed class TransitionSprintStatusHandler(ISprintLifecycleService service) : IRequestHandler<TransitionSprintStatusCommand>
{
    public Task Handle(TransitionSprintStatusCommand request, CancellationToken cancellationToken)
        => service.TransitionStatusAsync(request, cancellationToken);
}
