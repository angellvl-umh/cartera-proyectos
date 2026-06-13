using CarteraProyectos.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.Teams;

public record RemovePersonFromTeamCommand(int TeamId, int PersonId) : IRequest;

public sealed class RemovePersonFromTeamHandler(IAppDbContext db) : IRequestHandler<RemovePersonFromTeamCommand>
{
    public async Task Handle(RemovePersonFromTeamCommand request, CancellationToken cancellationToken)
    {
        var membership = await db.PersonTeamMemberships
            .FirstOrDefaultAsync(m => m.TeamId == request.TeamId && m.PersonId == request.PersonId, cancellationToken)
            ?? throw new KeyNotFoundException("El usuario no pertenece a este equipo.");

        db.PersonTeamMemberships.Remove(membership);
        await db.SaveChangesAsync(cancellationToken);
    }
}
