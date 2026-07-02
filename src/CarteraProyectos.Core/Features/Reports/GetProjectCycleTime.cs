using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.Reports;

// ── Query ─────────────────────────────────────────────────────────────────────

public record GetProjectCycleTimeQuery(int ProjectId) : IRequest<ProjectCycleTimeDto>;

// ── DTOs ──────────────────────────────────────────────────────────────────────

public record ProjectCycleTimeDto(
    int ProjectId,
    double? AverageCycleTimeDays,
    double? AverageLeadTimeDays,
    int CompletedItemsCount,
    IReadOnlyList<WorkItemCycleTimeDto> Items);

public record WorkItemCycleTimeDto(
    int WorkItemId,
    string Title,
    double? CycleTimeDays,
    double? LeadTimeDays,
    string DoneAt);

// ── Handler ───────────────────────────────────────────────────────────────────

public class GetProjectCycleTimeHandler(IAppDbContext db)
    : IRequestHandler<GetProjectCycleTimeQuery, ProjectCycleTimeDto>
{
    public async Task<ProjectCycleTimeDto> Handle(GetProjectCycleTimeQuery request, CancellationToken ct)
    {
        var projectExists = await db.Projects.AnyAsync(p => p.Id == request.ProjectId, ct);
        if (!projectExists)
            throw new KeyNotFoundException($"Proyecto {request.ProjectId} no encontrado.");

        // Tareas Done del proyecto
        var doneItems = await db.WorkItems
            .Where(wi => wi.ProjectId == request.ProjectId && wi.Status == WorkItemStatus.Done)
            .ToListAsync(ct);

        if (doneItems.Count == 0)
            return new ProjectCycleTimeDto(request.ProjectId, null, null, 0, []);

        var doneItemIds = doneItems.Select(wi => wi.Id).ToHashSet();

        // Una sola query de históricos para todas las tareas Done del proyecto
        var histories = await db.WorkItemStatusHistories
            .Where(h => doneItemIds.Contains(h.WorkItemId))
            .OrderBy(h => h.ChangedAt)
            .ToListAsync(ct);

        // Agrupa histórico por WorkItem
        var historiesByItem = histories
            .GroupBy(h => h.WorkItemId)
            .ToDictionary(g => g.Key, g => g.OrderBy(h => h.ChangedAt).ToList());

        var itemDtos = new List<WorkItemCycleTimeDto>();

        foreach (var wi in doneItems)
        {
            if (!historiesByItem.TryGetValue(wi.Id, out var itemHistories))
                continue; // sin histórico → skip (no tiene transición a Done registrada)

            // Transición a Done
            var doneEntry = itemHistories.FirstOrDefault(h => h.ToStatus == WorkItemStatus.Done);
            if (doneEntry is null)
                continue;

            var doneAt = doneEntry.ChangedAt;

            // Primera entrada del histórico (FromStatus == null → creación)
            var firstEntry = itemHistories.MinBy(h => h.ChangedAt);

            // Lead time: desde la primera entrada hasta Done
            double? leadTimeDays = firstEntry is not null
                ? Math.Round((doneAt - firstEntry.ChangedAt).TotalDays, 1)
                : null;

            // Cycle time: desde la primera transición a InProgress hasta Done
            var firstInProgress = itemHistories
                .Where(h => h.ToStatus == WorkItemStatus.InProgress)
                .OrderBy(h => h.ChangedAt)
                .FirstOrDefault();

            double? cycleTimeDays = firstInProgress is not null
                ? Math.Round((doneAt - firstInProgress.ChangedAt).TotalDays, 1)
                : null;

            itemDtos.Add(new WorkItemCycleTimeDto(
                wi.Id,
                wi.Title,
                cycleTimeDays,
                leadTimeDays,
                doneAt.ToString("yyyy-MM-ddTHH:mm:ssZ")));
        }

        // Ordena por fecha Done descendente, máximo 50
        var sortedItems = itemDtos
            .OrderByDescending(i => i.DoneAt)
            .Take(50)
            .ToList();

        var cycleTimes  = sortedItems.Where(i => i.CycleTimeDays.HasValue).Select(i => i.CycleTimeDays!.Value).ToList();
        var leadTimes   = sortedItems.Where(i => i.LeadTimeDays.HasValue).Select(i => i.LeadTimeDays!.Value).ToList();

        double? avgCycle = cycleTimes.Count > 0 ? Math.Round(cycleTimes.Average(), 1) : null;
        double? avgLead  = leadTimes.Count > 0  ? Math.Round(leadTimes.Average(), 1)  : null;

        return new ProjectCycleTimeDto(
            request.ProjectId,
            avgCycle,
            avgLead,
            itemDtos.Count,
            sortedItems);
    }
}
