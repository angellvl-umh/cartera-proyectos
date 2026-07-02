using CarteraProyectos.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.WorkItems;

public record ReorderWorkItemsCommand(int ProjectId, IReadOnlyList<int> OrderedIds) : IRequest;

public sealed class ReorderWorkItemsHandler(IAppDbContext db) : IRequestHandler<ReorderWorkItemsCommand>
{
    public async Task Handle(ReorderWorkItemsCommand request, CancellationToken cancellationToken)
    {
        var items = await db.WorkItems
            .Where(w => w.ProjectId == request.ProjectId && request.OrderedIds.Contains(w.Id))
            .ToListAsync(cancellationToken);

        if (items.Count != request.OrderedIds.Count)
            throw new KeyNotFoundException("Uno o más ids no pertenecen al proyecto o no existen.");

        for (var i = 0; i < request.OrderedIds.Count; i++)
        {
            var item = items.First(w => w.Id == request.OrderedIds[i]);
            item.SetSortOrder((i + 1) * 10);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
