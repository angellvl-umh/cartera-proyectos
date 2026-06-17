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
            var isLeadOfProjectTeam = await db.Teams
                .AnyAsync(t => teamIds.Contains(t.Id) && t.LeadPersonId == request.RequestingPersonId, cancellationToken);

            if (!isLeadOfProjectTeam)
                throw new UnauthorizedAccessException("El JefeEquipo debe liderar uno de los equipos asignados al proyecto.");
        }

        project.TransitionTo(request.NewStatus);
        await db.SaveChangesAsync(cancellationToken);
    }
}
