using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.Teams;

public record RemovePersonFromTeamCommand(int TeamId, int PersonId, int RequestingPersonId = 0) : IRequest;

public sealed class RemovePersonFromTeamHandler(IAppDbContext db) : IRequestHandler<RemovePersonFromTeamCommand>
{
    public async Task Handle(RemovePersonFromTeamCommand request, CancellationToken cancellationToken)
    {
        var requester = await db.Persons.FindAsync([request.RequestingPersonId], cancellationToken)
            ?? throw new KeyNotFoundException($"Persona con Id {request.RequestingPersonId} no encontrada.");
        if (requester.Role != PersonRole.Gestor)
            throw new UnauthorizedAccessException("Solo el Gestor puede eliminar personas de equipos.");

        var membership = await db.PersonTeamMemberships
            .FirstOrDefaultAsync(m => m.TeamId == request.TeamId && m.PersonId == request.PersonId, cancellationToken)
            ?? throw new KeyNotFoundException("El usuario no pertenece a este equipo.");

        db.PersonTeamMemberships.Remove(membership);
        await db.SaveChangesAsync(cancellationToken);
    }
}
