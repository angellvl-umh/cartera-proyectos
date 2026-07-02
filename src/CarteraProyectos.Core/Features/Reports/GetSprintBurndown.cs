using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.Reports;

// ── Query ─────────────────────────────────────────────────────────────────────

public record GetSprintBurndownQuery(int ProjectId, int SprintId) : IRequest<SprintBurndownDto>;

// ── DTOs ──────────────────────────────────────────────────────────────────────

public record SprintBurndownDto(
    int SprintId,
    string Name,
    string Status,
    string StartDate,
    string EndDate,
    int TotalPoints,
    IReadOnlyList<BurndownDayDto> Days);

public record BurndownDayDto(
    string Date,
    double IdealPoints,
    int? RemainingPoints);

// ── Handler ───────────────────────────────────────────────────────────────────

public class GetSprintBurndownHandler(IAppDbContext db)
    : IRequestHandler<GetSprintBurndownQuery, SprintBurndownDto>
{
    public async Task<SprintBurndownDto> Handle(GetSprintBurndownQuery request, CancellationToken ct)
    {
        var sprint = await db.Sprints
            .FirstOrDefaultAsync(s => s.Id == request.SprintId && s.ProjectId == request.ProjectId, ct);

        if (sprint is null)
            throw new KeyNotFoundException($"Sprint {request.SprintId} no encontrado en el proyecto {request.ProjectId}.");

        if (sprint.StartDate is null || sprint.EndDate is null)
            throw new InvalidOperationException(
                $"El sprint '{sprint.Name}' no tiene fechas de inicio y fin definidas. No se puede calcular el burndown.");

        var startDate = sprint.StartDate.Value;
        var endDate   = sprint.EndDate.Value;

        // Carga de WorkItems del sprint en una sola query
        var workItems = await db.WorkItems
            .Where(wi => wi.SprintId == sprint.Id)
            .ToListAsync(ct);

        var workItemIds = workItems.Select(wi => wi.Id).ToHashSet();

        // Carga de todos los históricos de estado de los WorkItems del sprint en una sola query
        List<WorkItemStatusHistory> histories = [];
        if (workItemIds.Count > 0)
        {
            histories = await db.WorkItemStatusHistories
                .Where(h => workItemIds.Contains(h.WorkItemId))
                .ToListAsync(ct);
        }

        // TotalPoints
        var totalPoints = sprint.CommittedPoints
            ?? workItems.Sum(wi => wi.EstimationPoints ?? 0);

        // Para cada WorkItem: fecha en que quedó "resuelto" (Done o Discarded → ya no es trabajo pendiente)
        // Usamos la transición a Done o Discarded más reciente para calcular cuándo dejó de contar.
        // La spec dice que Done y Discarded restan igual.
        var resolvedStatuses = new HashSet<WorkItemStatus>
        {
            WorkItemStatus.Done,
            WorkItemStatus.Discarded
        };

        // Por WorkItem: primera transición a Done/Discarded (consideramos también la más reciente
        // ya que Done es terminal; Discarded también lo es según TransitionStatus)
        var resolvedByWorkItem = histories
            .Where(h => resolvedStatuses.Contains(h.ToStatus))
            .GroupBy(h => h.WorkItemId)
            .ToDictionary(
                g => g.Key,
                g => g.Min(h => h.ChangedAt)); // primera vez que se resolvió

        var today = DateTime.UtcNow.Date;

        // Genera la línea de días
        var days = new List<BurndownDayDto>();
        int totalDays = (endDate.DayNumber - startDate.DayNumber); // número de intervalos

        for (var current = startDate; current <= endDate; current = current.AddDays(1))
        {
            var dayIndex = current.DayNumber - startDate.DayNumber; // 0 en startDate, totalDays en endDate
            
            double idealPoints = totalDays == 0
                ? 0.0
                : totalPoints * (1.0 - (double)dayIndex / totalDays);

            var currentDate = current.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).Date;
            bool isFuture = currentDate > today;

            int? remainingPoints = null;
            if (!isFuture)
            {
                // Puntos completados hasta este día (inclusive): suma de EstimationPoints ?? 0
                // de tareas cuya resolución ocurrió en o antes de este día
                var completedPoints = workItems
                    .Where(wi => resolvedByWorkItem.TryGetValue(wi.Id, out var resolvedAt)
                                 && resolvedAt.Date <= currentDate)
                    .Sum(wi => wi.EstimationPoints ?? 0);

                remainingPoints = totalPoints - completedPoints;
            }

            days.Add(new BurndownDayDto(
                current.ToString("yyyy-MM-dd"),
                Math.Round(idealPoints, 2),
                remainingPoints));
        }

        return new SprintBurndownDto(
            sprint.Id,
            sprint.Name,
            sprint.Status.ToString(),
            startDate.ToString("yyyy-MM-dd"),
            endDate.ToString("yyyy-MM-dd"),
            totalPoints,
            days);
    }
}
