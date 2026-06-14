using CarteraProyectos.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.WorkItems;

public record DeleteWorkItemCommand(int Id) : IRequest;

public sealed class DeleteWorkItemHandler(IAppDbContext db) : IRequestHandler<DeleteWorkItemCommand>
{
    public async Task Handle(DeleteWorkItemCommand request, CancellationToken cancellationToken)
    {
        var workItem = await db.WorkItems.FirstOrDefaultAsync(w => w.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Tarea {request.Id} no encontrada.");

        db.WorkItems.Remove(workItem);
        await db.SaveChangesAsync(cancellationToken);
    }
}
