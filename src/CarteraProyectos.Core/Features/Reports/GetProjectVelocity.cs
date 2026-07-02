using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.Reports;

// ── Query ─────────────────────────────────────────────────────────────────────

public record GetProjectVelocityQuery(int ProjectId) : IRequest<ProjectVelocityDto>;

// ── DTOs ──────────────────────────────────────────────────────────────────────

public record ProjectVelocityDto(
    int ProjectId,
    double? AverageVelocity,
    IReadOnlyList<SprintVelocityDto> Sprints);

public record SprintVelocityDto(
    int SprintId,
    string Name,
    string? StartDate,
    string? EndDate,
    int CommittedPoints,
    int DeliveredPoints,
    int? Capacity);

// ── Handler ───────────────────────────────────────────────────────────────────

public class GetProjectVelocityHandler(IAppDbContext db)
    : IRequestHandler<GetProjectVelocityQuery, ProjectVelocityDto>
{
    public async Task<ProjectVelocityDto> Handle(GetProjectVelocityQuery request, CancellationToken ct)
    {
        var projectExists = await db.Projects.AnyAsync(p => p.Id == request.ProjectId, ct);
        if (!projectExists)
            throw new KeyNotFoundException($"Proyecto {request.ProjectId} no encontrado.");

        // Sprints completados del proyecto ordenados cronológicamente
        var sprints = await db.Sprints
            .Where(s => s.ProjectId == request.ProjectId && s.Status == SprintStatus.Completed)
            .OrderBy(s => s.StartDate)
            .ThenBy(s => s.Id)
            .ToListAsync(ct);

        if (sprints.Count == 0)
            return new ProjectVelocityDto(request.ProjectId, null, []);

        // IDs de sprints que necesitan cálculo al vuelo (snapshot null)
        var sprintIdsNeedingFlyCalc = sprints
            .Where(s => s.CommittedPoints is null || s.DeliveredPoints is null)
            .Select(s => s.Id)
            .ToHashSet();

        // Carga workitems solo para los sprints que lo necesitan (evitar N+1)
        Dictionary<int, List<WorkItem>> workItemsBySprint = [];
        if (sprintIdsNeedingFlyCalc.Count > 0)
        {
            var items = await db.WorkItems
                .Where(wi => wi.SprintId.HasValue && sprintIdsNeedingFlyCalc.Contains(wi.SprintId.Value))
                .ToListAsync(ct);

            workItemsBySprint = items
                .GroupBy(wi => wi.SprintId!.Value)
                .ToDictionary(g => g.Key, g => g.ToList());
        }

        var sprintDtos = sprints.Select(s =>
        {
            int committed;
            int delivered;

            if (s.CommittedPoints.HasValue && s.DeliveredPoints.HasValue)
            {
                committed = s.CommittedPoints.Value;
                delivered = s.DeliveredPoints.Value;
            }
            else
            {
                // Cálculo al vuelo para sprints antiguos sin snapshot
                var items = workItemsBySprint.TryGetValue(s.Id, out var wi) ? wi : [];
                committed = items.Sum(i => i.EstimationPoints ?? 0);
                delivered = items.Where(i => i.Status == WorkItemStatus.Done)
                                 .Sum(i => i.EstimationPoints ?? 0);
            }

            return new SprintVelocityDto(
                s.Id,
                s.Name,
                s.StartDate?.ToString("yyyy-MM-dd"),
                s.EndDate?.ToString("yyyy-MM-dd"),
                committed,
                delivered,
                s.Capacity);
        }).ToList();

        var averageVelocity = sprintDtos.Count > 0
            ? sprintDtos.Average(s => (double)s.DeliveredPoints)
            : (double?)null;

        return new ProjectVelocityDto(request.ProjectId, averageVelocity, sprintDtos);
    }
}
