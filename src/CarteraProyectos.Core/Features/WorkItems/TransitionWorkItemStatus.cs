using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.WorkItems;

public record TransitionWorkItemStatusCommand(int Id, WorkItemStatus NewStatus) : IRequest;

public sealed class TransitionWorkItemStatusHandler(IAppDbContext db) : IRequestHandler<TransitionWorkItemStatusCommand>
{
    public async Task Handle(TransitionWorkItemStatusCommand request, CancellationToken cancellationToken)
    {
        var workItem = await db.WorkItems.FirstOrDefaultAsync(w => w.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Tarea {request.Id} no encontrada.");

        workItem.TransitionStatus(request.NewStatus);
        await db.SaveChangesAsync(cancellationToken);
    }
}
