using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.Sprints;

public interface ISprintLifecycleService
{
    Task<int> CreateAsync(CreateSprintCommand command, CancellationToken ct);
    Task TransitionStatusAsync(TransitionSprintStatusCommand command, CancellationToken ct);
}

public sealed class SprintLifecycleService(IAppDbContext db) : ISprintLifecycleService
{
    public async Task<int> CreateAsync(CreateSprintCommand request, CancellationToken ct)
    {
        var projectExists = await db.Projects.AnyAsync(p => p.Id == request.ProjectId, ct);
        if (!projectExists) throw new KeyNotFoundException($"Proyecto {request.ProjectId} no encontrado.");

        var sprint = Sprint.Create(request.ProjectId, request.Name, request.Goal,
            request.StartDate, request.EndDate, request.Capacity);
        db.Sprints.Add(sprint);

        db.SprintStatusHistories.Add(
            SprintStatusHistory.Create(sprint, null, sprint.Status, request.RequestingPersonId));

        await db.SaveChangesAsync(ct);
        return sprint.Id;
    }

    public async Task TransitionStatusAsync(TransitionSprintStatusCommand request, CancellationToken ct)
    {
        var sprint = await db.Sprints.FirstOrDefaultAsync(s => s.Id == request.Id, ct)
            ?? throw new KeyNotFoundException($"Sprint {request.Id} no encontrado.");

        if (request.NewStatus == SprintStatus.Active)
        {
            var hasActiveSprint = await db.Sprints.AnyAsync(
                s => s.ProjectId == sprint.ProjectId && s.Status == SprintStatus.Active && s.Id != sprint.Id,
                ct);
            if (hasActiveSprint)
                throw new InvalidOperationException("Ya existe un sprint activo en este proyecto. Complétalo antes de iniciar uno nuevo.");

            // Snapshot de puntos comprometidos al activar
            var committed = await db.WorkItems
                .Where(w => w.SprintId == sprint.Id)
                .SumAsync(w => w.EstimationPoints ?? 0, ct);
            sprint.SnapshotCommitted(committed);
        }

        if (request.NewStatus == SprintStatus.Completed)
        {
            // Snapshot de puntos entregados
            var delivered = await db.WorkItems
                .Where(w => w.SprintId == sprint.Id && w.Status == WorkItemStatus.Done)
                .SumAsync(w => w.EstimationPoints ?? 0, ct);
            sprint.SnapshotDelivered(delivered);

            // Tareas sin terminar
            var unfinished = await db.WorkItems
                .Where(w => w.SprintId == sprint.Id
                         && w.Status != WorkItemStatus.Done
                         && w.Status != WorkItemStatus.Discarded)
                .ToListAsync(ct);

            if (unfinished.Count > 0)
            {
                if (request.CarryOver is null)
                    throw new InvalidOperationException(
                        "El sprint tiene tareas sin terminar. Indica un destino de carry-over (Backlog o Sprint) para completarlo.");

                if (request.CarryOver == CarryOverTarget.Backlog)
                {
                    foreach (var wi in unfinished)
                    {
                        var prevStatus = wi.Status;
                        if (prevStatus != WorkItemStatus.Backlog)
                        {
                            wi.TransitionStatus(WorkItemStatus.Backlog);
                            db.WorkItemStatusHistories.Add(
                                WorkItemStatusHistory.Create(wi, prevStatus, WorkItemStatus.Backlog, request.RequestingPersonId));
                        }
                        wi.AssignToSprint(null);
                    }
                }
                else // CarryOver == Sprint
                {
                    if (request.TargetSprintId is null)
                        throw new InvalidOperationException(
                            "CarryOver = Sprint requiere indicar TargetSprintId.");

                    if (request.TargetSprintId == sprint.Id)
                        throw new InvalidOperationException(
                            "El sprint destino del carry-over no puede ser el mismo sprint que se está completando.");

                    var targetSprint = await db.Sprints.FirstOrDefaultAsync(
                        s => s.Id == request.TargetSprintId, ct)
                        ?? throw new InvalidOperationException(
                            $"Sprint destino {request.TargetSprintId} no encontrado.");

                    if (targetSprint.ProjectId != sprint.ProjectId)
                        throw new InvalidOperationException(
                            "El sprint destino debe pertenecer al mismo proyecto.");

                    if (targetSprint.Status != SprintStatus.Planning)
                        throw new InvalidOperationException(
                            "El sprint destino debe estar en estado Planning para recibir el carry-over.");

                    foreach (var wi in unfinished)
                        wi.AssignToSprint(request.TargetSprintId);
                }
            }
        }

        var oldStatus = sprint.Status;
        sprint.TransitionStatus(request.NewStatus);

        db.SprintStatusHistories.Add(
            SprintStatusHistory.Create(sprint, oldStatus, request.NewStatus, request.RequestingPersonId));

        await db.SaveChangesAsync(ct);
    }
}
