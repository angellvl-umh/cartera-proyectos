using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Features.Projects;
using CarteraProyectos.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.WorkItems;

public interface IWorkItemLifecycleService
{
    Task TransitionStatusAsync(int id, WorkItemStatus newStatus, int requestingPersonId, CancellationToken ct);
    Task ReorderAsync(int projectId, IReadOnlyList<int> orderedIds, CancellationToken ct);
    Task BulkAssignToSprintAsync(int projectId, IReadOnlyList<int> workItemIds, int? sprintId, CancellationToken ct);
}

public sealed class WorkItemLifecycleService(IAppDbContext db) : IWorkItemLifecycleService
{
    public async Task TransitionStatusAsync(int id, WorkItemStatus newStatus, int requestingPersonId, CancellationToken ct)
    {
        var workItem = await db.WorkItems
            .FirstOrDefaultAsync(w => w.Id == id, ct)
            ?? throw new KeyNotFoundException($"Tarea {id} no encontrada.");

        if (requestingPersonId > 0)
        {
            var requester = await db.Persons.FindAsync([requestingPersonId], ct)
                ?? throw new KeyNotFoundException($"Persona con Id {requestingPersonId} no encontrada.");

            await ProjectAuthorization.EnsureCanManageProjectAsync(db, workItem.ProjectId, requester, ct);
        }

        var oldStatus = workItem.Status;
        workItem.TransitionStatus(newStatus);

        db.WorkItemStatusHistories.Add(
            WorkItemStatusHistory.Create(workItem, oldStatus, newStatus, requestingPersonId));

        await db.SaveChangesAsync(ct);
    }

    public async Task ReorderAsync(int projectId, IReadOnlyList<int> orderedIds, CancellationToken ct)
    {
        var items = await db.WorkItems
            .Where(w => w.ProjectId == projectId && orderedIds.Contains(w.Id))
            .ToListAsync(ct);

        if (items.Count != orderedIds.Count)
            throw new KeyNotFoundException("Uno o más ids no pertenecen al proyecto o no existen.");

        for (var i = 0; i < orderedIds.Count; i++)
        {
            var item = items.First(w => w.Id == orderedIds[i]);
            item.SetSortOrder((i + 1) * 10);
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task BulkAssignToSprintAsync(int projectId, IReadOnlyList<int> workItemIds, int? sprintId, CancellationToken ct)
    {
        // Validar sprint si se proporciona (una sola vez para el lote)
        if (sprintId.HasValue)
        {
            var sprintExists = await db.Sprints.AnyAsync(
                s => s.Id == sprintId.Value && s.ProjectId == projectId,
                ct);

            if (!sprintExists)
                throw new KeyNotFoundException($"Sprint {sprintId} no encontrado en el proyecto {projectId}.");
        }

        // Cargar todos los items que pertenezcan al proyecto
        var items = await db.WorkItems
            .Where(w => w.ProjectId == projectId && workItemIds.Contains(w.Id))
            .ToListAsync(ct);

        // Verificar que todos los ids existen y son del proyecto
        if (items.Count != workItemIds.Distinct().Count())
            throw new KeyNotFoundException("Uno o más WorkItemIds no pertenecen al proyecto o no existen.");

        foreach (var item in items)
            item.AssignToSprint(sprintId);

        await db.SaveChangesAsync(ct);
    }
}
