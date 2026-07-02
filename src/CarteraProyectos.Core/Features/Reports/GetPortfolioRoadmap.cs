using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.Reports;

// ── Query ─────────────────────────────────────────────────────────────────────

public record GetPortfolioRoadmapQuery(int? Year = null) : IRequest<PortfolioRoadmapDto>;

// ── DTOs ──────────────────────────────────────────────────────────────────────

public record PortfolioRoadmapDto(
    int Year,
    IReadOnlyList<RoadmapTeamDto> Teams,
    IReadOnlyList<RoadmapProjectDto> Unassigned,
    IReadOnlyList<RoadmapProjectDto> Undated,
    IReadOnlyList<int> AvailableYears);

public record RoadmapTeamDto(
    int TeamId,
    string TeamName,
    IReadOnlyList<RoadmapProjectDto> Projects);

public record RoadmapProjectDto(
    int Id,
    string Title,
    string Status,
    string Complexity,
    int? BusinessValue,
    string? StartDate,
    string? EndDate,
    string? DesiredDeploymentDate,
    IReadOnlyList<RoadmapMilestoneDto> Milestones);

/// <param name="Reached">true si la tarea hito está en estado Done.</param>
public record RoadmapMilestoneDto(
    int Id,
    string Title,
    string? HitoDate,
    bool Reached);

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class GetPortfolioRoadmapHandler(IAppDbContext db)
    : IRequestHandler<GetPortfolioRoadmapQuery, PortfolioRoadmapDto>
{
    public async Task<PortfolioRoadmapDto> Handle(
        GetPortfolioRoadmapQuery request, CancellationToken ct)
    {
        var year = request.Year ?? DateTime.UtcNow.Year;

        // ── Años disponibles (para selector de año en UI) ─────────────────────
        var availableYears = await db.Projects
            .Where(p => p.PortfolioYear != null)
            .Select(p => p.PortfolioYear!.Value)
            .Distinct()
            .OrderByDescending(y => y)
            .ToListAsync(ct);

        // ── Cargar todos los proyectos candidatos ─────────────────────────────
        // Incluimos los que tienen PortfolioYear == year o cuyo rango de fechas solape.
        // Excluimos Completed que terminaron antes del año consultado.
        var allProjects = await db.Projects.ToListAsync(ct);

        var yearStart = new DateOnly(year, 1, 1);
        var yearEnd   = new DateOnly(year, 12, 31);

        var projects = allProjects.Where(p =>
        {
            // Excluir Completed que terminaron antes del año consultado
            if (p.Status == ProjectStatus.Completed)
            {
                var effectiveEnd = p.EndDate ?? p.DesiredDeploymentDate;
                if (effectiveEnd.HasValue && effectiveEnd.Value < yearStart)
                    return false;
            }

            // Incluir si PortfolioYear coincide
            if (p.PortfolioYear == year)
                return true;

            // Incluir si el rango [StartDate, EndDate ?? DesiredDeploymentDate ?? StartDate] solapa el año
            if (p.StartDate.HasValue)
            {
                var rangeStart = p.StartDate.Value;
                var rangeEnd   = p.EndDate ?? p.DesiredDeploymentDate ?? p.StartDate.Value;
                // Solapamiento: rangeStart <= yearEnd && rangeEnd >= yearStart
                return rangeStart <= yearEnd && rangeEnd >= yearStart;
            }

            return false;
        }).ToList();

        var projectIds = projects.Select(p => p.Id).ToList();

        // ── Asignaciones de equipos (sin N+1) ─────────────────────────────────
        var assignments = await db.ProjectTeamAssignments
            .Where(a => projectIds.Contains(a.ProjectId))
            .Select(a => new
            {
                a.ProjectId,
                a.TeamId,
                a.IsPrimary,
                TeamName = a.Team!.Name
            })
            .ToListAsync(ct);

        // ── Hitos (WorkItems con IsHito) ──────────────────────────────────────
        var milestones = await db.WorkItems
            .Where(w => projectIds.Contains(w.ProjectId) && w.IsHito)
            .Select(w => new
            {
                w.Id,
                w.ProjectId,
                w.Title,
                w.HitoDate,
                Reached = w.Status == WorkItemStatus.Done
            })
            .ToListAsync(ct);

        // ── Agrupar hitos por proyecto ────────────────────────────────────────
        var milestonesByProject = milestones
            .GroupBy(m => m.ProjectId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<RoadmapMilestoneDto>)g
                    .Select(m => new RoadmapMilestoneDto(
                        m.Id,
                        m.Title,
                        m.HitoDate?.ToString("yyyy-MM-dd"),
                        m.Reached))
                    .ToList());

        // ── Construir RoadmapProjectDto ───────────────────────────────────────
        RoadmapProjectDto ToDto(Project p) => new(
            p.Id,
            p.Title,
            p.Status.ToString(),
            p.Complexity.ToString(),
            p.BusinessValue,
            p.StartDate?.ToString("yyyy-MM-dd"),
            p.EndDate?.ToString("yyyy-MM-dd"),
            p.DesiredDeploymentDate?.ToString("yyyy-MM-dd"),
            milestonesByProject.TryGetValue(p.Id, out var m) ? m : []);

        // ── Clasificar proyectos ──────────────────────────────────────────────
        var undated     = new List<RoadmapProjectDto>();
        var unassigned  = new List<RoadmapProjectDto>();
        // teamId → (teamName, proyectos)
        var byTeam      = new Dictionary<int, (string Name, List<RoadmapProjectDto> Projects)>();

        foreach (var p in projects)
        {
            var dto = ToDto(p);

            // Sin StartDate → Undated
            if (!p.StartDate.HasValue)
            {
                undated.Add(dto);
                continue;
            }

            // Determinar equipo: primario > primero disponible > sin equipo
            var projectAssignments = assignments
                .Where(a => a.ProjectId == p.Id)
                .ToList();

            var teamAssignment = projectAssignments.FirstOrDefault(a => a.IsPrimary)
                              ?? projectAssignments.FirstOrDefault();

            if (teamAssignment is null)
            {
                unassigned.Add(dto);
            }
            else
            {
                if (!byTeam.TryGetValue(teamAssignment.TeamId, out var entry))
                {
                    entry = (teamAssignment.TeamName, []);
                    byTeam[teamAssignment.TeamId] = entry;
                }
                entry.Projects.Add(dto);
            }
        }

        // ── Construir lista de equipos ordenada por nombre ────────────────────
        var teamDtos = byTeam
            .Select(kv => new RoadmapTeamDto(
                kv.Key,
                kv.Value.Name,
                kv.Value.Projects
                    .OrderBy(p => p.StartDate)
                    .ToList()))
            .OrderBy(t => t.TeamName)
            .ToList();

        return new PortfolioRoadmapDto(
            year,
            teamDtos,
            unassigned.OrderBy(p => p.StartDate).ToList(),
            undated.OrderBy(p => p.Title).ToList(),
            availableYears);
    }
}
