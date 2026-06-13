using CarteraProyectos.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.Projects;

public record RemoveTeamFromProjectCommand(int ProjectId, int TeamId) : IRequest;

public sealed class RemoveTeamFromProjectHandler(IAppDbContext db) : IRequestHandler<RemoveTeamFromProjectCommand>
{
    public async Task Handle(RemoveTeamFromProjectCommand request, CancellationToken cancellationToken)
    {
        var assignment = await db.ProjectTeamAssignments
            .FirstOrDefaultAsync(a => a.ProjectId == request.ProjectId && a.TeamId == request.TeamId, cancellationToken)
            ?? throw new KeyNotFoundException("El equipo no estÃ¡ asignado a este proyecto.");

        db.ProjectTeamAssignments.Remove(assignment);
        await db.SaveChangesAsync(cancellationToken);
    }
}
