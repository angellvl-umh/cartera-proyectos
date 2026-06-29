using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.WorkItems;

public record TransitionWorkItemStatusCommand(int Id, WorkItemStatus NewStatus, int RequestingPersonId = 0) : IRequest;

public sealed class TransitionWorkItemStatusHandler(IAppDbContext db) : IRequestHandler<TransitionWorkItemStatusCommand>
{
    public async Task Handle(TransitionWorkItemStatusCommand request, CancellationToken cancellationToken)
    {
        var workItem = await db.WorkItems
            .Include(w => w.Assignees)
            .FirstOrDefaultAsync(w => w.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Tarea {request.Id} no encontrada.");

        if (request.RequestingPersonId > 0)
        {
            var requester = await db.Persons.FindAsync([request.RequestingPersonId], cancellationToken)
                ?? throw new KeyNotFoundException($"Persona con Id {request.RequestingPersonId} no encontrada.");

            if (requester.Role == PersonRole.JefeEquipo)
            {
                var teamIds = await db.ProjectTeamAssignments
                    .Where(a => a.ProjectId == workItem.ProjectId)
                    .Select(a => a.TeamId)
                    .ToListAsync(cancellationToken);
                var isLeadOfProjectTeam = await db.Teams
                    .AnyAsync(t => teamIds.Contains(t.Id) && t.LeadPersonId == request.RequestingPersonId, cancellationToken);
                if (!isLeadOfProjectTeam)
                    throw new UnauthorizedAccessException("El JefeEquipo debe liderar uno de los equipos asignados al proyecto.");
            }
            else if (requester.Role == PersonRole.Desarrollador)
            {
                var isAssignee = workItem.Assignees.Any(a => a.Id == request.RequestingPersonId);
                if (!isAssignee)
                    throw new UnauthorizedAccessException("Los Desarrolladores solo pueden cambiar el estado de sus propias tareas.");
            }
        }

        var oldStatus = workItem.Status;
        workItem.TransitionStatus(request.NewStatus);

        db.WorkItemStatusHistories.Add(
            WorkItemStatusHistory.Create(workItem, oldStatus, request.NewStatus, request.RequestingPersonId));

        await db.SaveChangesAsync(cancellationToken);
    }
}
