using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.Teams;

public record DeleteTeamCommand(int Id, int RequestingPersonId = 0) : IRequest;

public sealed class DeleteTeamHandler(IAppDbContext db) : IRequestHandler<DeleteTeamCommand>
{
    private static readonly ProjectStatus[] ActiveStatuses =
        [ProjectStatus.Approved, ProjectStatus.InProgress, ProjectStatus.Paused];

    public async Task Handle(DeleteTeamCommand request, CancellationToken cancellationToken)
    {
        var requester = await db.Persons.FindAsync([request.RequestingPersonId], cancellationToken)
            ?? throw new KeyNotFoundException($"Persona con Id {request.RequestingPersonId} no encontrada.");
        if (requester.Role != PersonRole.Gestor)
            throw new UnauthorizedAccessException("Solo el Gestor puede eliminar equipos.");

        var team = await db.Teams.FindAsync([request.Id], cancellationToken)
            ?? throw new KeyNotFoundException($"Equipo con Id {request.Id} no encontrado.");

        var hasActiveProjects = await db.ProjectTeamAssignments
            .AnyAsync(a => a.TeamId == request.Id
                        && ActiveStatuses.Contains(a.Project!.Status), cancellationToken);

        if (hasActiveProjects)
            throw new InvalidOperationException("No se puede eliminar un equipo con proyectos activos.");

        db.Teams.Remove(team);
        await db.SaveChangesAsync(cancellationToken);
    }
}
