using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.Projects;

public record AssignTeamToProjectCommand(int ProjectId, int TeamId, bool IsPrimary, int RequestingPersonId = 0) : IRequest;

public sealed class AssignTeamToProjectHandler(IAppDbContext db) : IRequestHandler<AssignTeamToProjectCommand>
{
    public async Task Handle(AssignTeamToProjectCommand request, CancellationToken cancellationToken)
    {
        var requester = await db.Persons.FindAsync([request.RequestingPersonId], cancellationToken)
            ?? throw new KeyNotFoundException($"Persona con Id {request.RequestingPersonId} no encontrada.");
        if (requester.Role != PersonRole.Gestor)
            throw new UnauthorizedAccessException("Solo el Gestor puede asignar equipos a proyectos.");

        if (!await db.Projects.AnyAsync(p => p.Id == request.ProjectId, cancellationToken))
            throw new KeyNotFoundException($"Proyecto con Id {request.ProjectId} no encontrado.");

        if (!await db.Teams.AnyAsync(t => t.Id == request.TeamId, cancellationToken))
            throw new KeyNotFoundException($"Equipo con Id {request.TeamId} no encontrado.");

        var existing = await db.ProjectTeamAssignments
            .FirstOrDefaultAsync(a => a.ProjectId == request.ProjectId && a.TeamId == request.TeamId, cancellationToken);

        if (existing is not null)
        {
            existing.SetPrimary(request.IsPrimary);
        }
        else
        {
            db.ProjectTeamAssignments.Add(
                ProjectTeamAssignment.Create(request.ProjectId, request.TeamId, request.IsPrimary));
        }

        // Solo puede haber un equipo primario
        if (request.IsPrimary)
        {
            var others = await db.ProjectTeamAssignments
                .Where(a => a.ProjectId == request.ProjectId && a.TeamId != request.TeamId && a.IsPrimary)
                .ToListAsync(cancellationToken);
            foreach (var other in others) other.SetPrimary(false);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
