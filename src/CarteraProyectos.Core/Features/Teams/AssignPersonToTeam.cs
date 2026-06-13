using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.Teams;

public record AssignPersonToTeamCommand(int TeamId, int PersonId) : IRequest;

public sealed class AssignPersonToTeamHandler(IAppDbContext db) : IRequestHandler<AssignPersonToTeamCommand>
{
    public async Task Handle(AssignPersonToTeamCommand request, CancellationToken cancellationToken)
    {
        if (!await db.Teams.AnyAsync(t => t.Id == request.TeamId, cancellationToken))
            throw new KeyNotFoundException($"Equipo con Id {request.TeamId} no encontrado.");

        if (!await db.Persons.AnyAsync(p => p.Id == request.PersonId, cancellationToken))
            throw new KeyNotFoundException($"Persona con Id {request.PersonId} no encontrada.");

        var alreadyMember = await db.PersonTeamMemberships
            .AnyAsync(m => m.TeamId == request.TeamId && m.PersonId == request.PersonId, cancellationToken);

        if (alreadyMember) return;

        db.PersonTeamMemberships.Add(PersonTeamMembership.Create(request.PersonId, request.TeamId));
        await db.SaveChangesAsync(cancellationToken);
    }
}
