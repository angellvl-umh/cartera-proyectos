using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.Projects;

public record TransitionProjectStatusCommand(int ProjectId, ProjectStatus NewStatus, int RequestingPersonId) : IRequest;

public sealed class TransitionProjectStatusHandler(IAppDbContext db)
    : IRequestHandler<TransitionProjectStatusCommand>
{
    public async Task Handle(TransitionProjectStatusCommand request, CancellationToken cancellationToken)
    {
        var project = await db.Projects
            .Include(p => p.Teams)
            .FirstOrDefaultAsync(p => p.Id == request.ProjectId, cancellationToken)
            ?? throw new KeyNotFoundException($"Proyecto con Id {request.ProjectId} no encontrado.");

        var person = await db.Persons.FindAsync([request.RequestingPersonId], cancellationToken)
            ?? throw new KeyNotFoundException($"Persona con Id {request.RequestingPersonId} no encontrada.");

        if (person.Role == PersonRole.Desarrollador)
            throw new UnauthorizedAccessException("Los Desarrolladores no pueden cambiar el estado del proyecto.");

        if (person.Role == PersonRole.JefeEquipo)
        {
            var teamIds = project.Teams.Select(t => t.TeamId).ToHashSet();
            var isAssigned = await db.PersonTeamMemberships
                .AnyAsync(m => teamIds.Contains(m.TeamId) && m.PersonId == request.RequestingPersonId, cancellationToken);

            if (!isAssigned)
                throw new UnauthorizedAccessException("El JefeEquipo debe pertenecer a un equipo asignado al proyecto.");
        }

        project.TransitionTo(request.NewStatus);
        await db.SaveChangesAsync(cancellationToken);
    }
}
