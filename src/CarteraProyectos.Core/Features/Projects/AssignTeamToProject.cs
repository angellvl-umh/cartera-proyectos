using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.Projects;

public record AssignTeamToProjectCommand(int ProjectId, int TeamId, bool IsPrimary, int RequestingPersonId = 0) : IRequest;

public sealed class AssignTeamToProjectHandler(IProjectLifecycleService service) : IRequestHandler<AssignTeamToProjectCommand>
{
    public Task Handle(AssignTeamToProjectCommand request, CancellationToken cancellationToken)
        => service.AssignTeamAsync(request, cancellationToken);
}
