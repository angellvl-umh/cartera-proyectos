using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.Projects;

public record TransitionProjectStatusCommand(int ProjectId, ProjectStatus NewStatus, int RequestingPersonId) : IRequest;

public sealed class TransitionProjectStatusHandler(IProjectLifecycleService service)
    : IRequestHandler<TransitionProjectStatusCommand>
{
    public Task Handle(TransitionProjectStatusCommand request, CancellationToken cancellationToken)
        => service.TransitionStatusAsync(request, cancellationToken);
}
