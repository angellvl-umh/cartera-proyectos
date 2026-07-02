using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Features.Projects;
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
            .FirstOrDefaultAsync(w => w.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Tarea {request.Id} no encontrada.");

        if (request.RequestingPersonId > 0)
        {
            var requester = await db.Persons.FindAsync([request.RequestingPersonId], cancellationToken)
                ?? throw new KeyNotFoundException($"Persona con Id {request.RequestingPersonId} no encontrada.");

            await ProjectAuthorization.EnsureCanManageProjectAsync(db, workItem.ProjectId, requester, cancellationToken);
        }

        var oldStatus = workItem.Status;
        workItem.TransitionStatus(request.NewStatus);

        db.WorkItemStatusHistories.Add(
            WorkItemStatusHistory.Create(workItem, oldStatus, request.NewStatus, request.RequestingPersonId));

        await db.SaveChangesAsync(cancellationToken);
    }
}
