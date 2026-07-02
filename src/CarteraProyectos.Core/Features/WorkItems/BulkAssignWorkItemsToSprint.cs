using CarteraProyectos.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.WorkItems;

public record BulkAssignWorkItemsToSprintCommand(
    int ProjectId,
    IReadOnlyList<int> WorkItemIds,
    int? SprintId) : IRequest;

public sealed class BulkAssignWorkItemsToSprintHandler(IAppDbContext db)
    : IRequestHandler<BulkAssignWorkItemsToSprintCommand>
{
    public async Task Handle(BulkAssignWorkItemsToSprintCommand request, CancellationToken cancellationToken)
    {
        // Validar sprint si se proporciona (una sola vez para el lote)
        if (request.SprintId.HasValue)
        {
            var sprintExists = await db.Sprints.AnyAsync(
                s => s.Id == request.SprintId.Value && s.ProjectId == request.ProjectId,
                cancellationToken);

            if (!sprintExists)
                throw new KeyNotFoundException($"Sprint {request.SprintId} no encontrado en el proyecto {request.ProjectId}.");
        }

        // Cargar todos los items que pertenezcan al proyecto
        var items = await db.WorkItems
            .Where(w => w.ProjectId == request.ProjectId && request.WorkItemIds.Contains(w.Id))
            .ToListAsync(cancellationToken);

        // Verificar que todos los ids existen y son del proyecto
        if (items.Count != request.WorkItemIds.Distinct().Count())
            throw new KeyNotFoundException("Uno o más WorkItemIds no pertenecen al proyecto o no existen.");

        foreach (var item in items)
            item.AssignToSprint(request.SprintId);

        await db.SaveChangesAsync(cancellationToken);
    }
}
