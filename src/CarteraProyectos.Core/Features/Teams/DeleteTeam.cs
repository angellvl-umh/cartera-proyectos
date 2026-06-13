using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.Teams;

public record DeleteTeamCommand(int Id) : IRequest;

public sealed class DeleteTeamHandler(IAppDbContext db) : IRequestHandler<DeleteTeamCommand>
{
    private static readonly ProjectStatus[] ActiveStatuses =
        [ProjectStatus.Approved, ProjectStatus.InProgress, ProjectStatus.Paused];

    public async Task Handle(DeleteTeamCommand request, CancellationToken cancellationToken)
    {
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
