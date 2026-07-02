using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.Reports;

// ── Query ─────────────────────────────────────────────────────────────────────

public record GetCapacityForecastQuery(int? Year = null) : IRequest<CapacityForecastDto>;

// ── DTOs ──────────────────────────────────────────────────────────────────────

public record CapacityForecastDto(
    int Year,
    string MethodologyNote,
    IReadOnlyList<ForecastTeamDto> Teams);

public record ForecastTeamDto(
    int TeamId,
    string TeamName,
    int MemberCount,
    IReadOnlyList<ForecastQuarterDto> Quarters);

/// <param name="Level">Green (&lt;70 %), Yellow (70–100 %), Red (&gt;100 %).</param>
public record ForecastQuarterDto(
    int Quarter,
    double DemandPersonMonths,
    double CapacityPersonMonths,
    int LoadPercent,
    string Level,
    IReadOnlyList<string> ProjectTitles);

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class GetCapacityForecastHandler(IAppDbContext db)
    : IRequestHandler<GetCapacityForecastQuery, CapacityForecastDto>
{
    // Esfuerzo total por complejidad (persona-mes)
    private static readonly Dictionary<ProjectComplexity, double> EffortByComplexity = new()
    {
        [ProjectComplexity.VerySmall] = 1,
        [ProjectComplexity.Small]     = 2,
        [ProjectComplexity.Medium]    = 4,
        [ProjectComplexity.Large]     = 16,
        [ProjectComplexity.VeryLarge] = 24,
    };

    // Duración por defecto cuando no hay fecha fin (meses)
    private static readonly Dictionary<ProjectComplexity, double> DefaultDurationByComplexity = new()
    {
        [ProjectComplexity.VerySmall] = 0.5,
        [ProjectComplexity.Small]     = 1,
        [ProjectComplexity.Medium]    = 2,
        [ProjectComplexity.Large]     = 4,
        [ProjectComplexity.VeryLarge] = 6,
    };

    // Estados excluidos del forecast
    private static readonly ProjectStatus[] ExcludedStatuses =
    [
        ProjectStatus.Completed,
        ProjectStatus.Stopped,
        ProjectStatus.PostponedByClient,
    ];

    private const double AvailabilityFactor = 0.8;
    private const double DaysPerMonth = 30.44;

    public async Task<CapacityForecastDto> Handle(
        GetCapacityForecastQuery request, CancellationToken ct)
    {
        var year = request.Year ?? DateTime.UtcNow.Year;

        // ── Equipos con sus miembros ──────────────────────────────────────────
        var teams = await db.Teams
            .Include(t => t.Members)
            .OrderBy(t => t.Name)
            .ToListAsync(ct);

        // ── Proyectos activos con sus asignaciones de equipo ──────────────────
        var projects = await db.Projects
            .Where(p => !ExcludedStatuses.Contains(p.Status) && p.StartDate != null)
            .ToListAsync(ct);

        var projectIds = projects.Select(p => p.Id).ToList();

        // Número de equipos asignados por proyecto
        var teamCountByProject = await db.ProjectTeamAssignments
            .Where(a => projectIds.Contains(a.ProjectId))
            .GroupBy(a => a.ProjectId)
            .Select(g => new { ProjectId = g.Key, TeamCount = g.Count() })
            .ToListAsync(ct);

        // Qué proyectos están asignados a cada equipo
        var assignmentsByTeam = await db.ProjectTeamAssignments
            .Where(a => projectIds.Contains(a.ProjectId))
            .Select(a => new { a.ProjectId, a.TeamId })
            .ToListAsync(ct);

        // ── Pre-calcular ritmo mensual por proyecto ───────────────────────────
        var projectRhythm = new Dictionary<int, (double MonthlyRate, string Title)>();

        foreach (var p in projects)
        {
            // StartDate garantizado por el filtro
            var startDate = p.StartDate!.Value;
            var endDateOpt = p.EndDate ?? p.DesiredDeploymentDate;

            double durationMonths;
            if (endDateOpt.HasValue)
            {
                var days = (endDateOpt.Value.ToDateTime(TimeOnly.MinValue)
                          - startDate.ToDateTime(TimeOnly.MinValue)).TotalDays;
                // Al menos 0.5 meses para evitar división por cero en proyectos con fechas idénticas
                durationMonths = Math.Max(days / DaysPerMonth, 0.5);
            }
            else
            {
                durationMonths = DefaultDurationByComplexity[p.Complexity];
            }

            var effort      = EffortByComplexity[p.Complexity];
            var monthlyRate = effort / durationMonths;
            projectRhythm[p.Id] = (monthlyRate, p.Title);
        }

        // ── Calcular demanda por (teamId, quarter) ────────────────────────────
        // quarter 1..4: meses [1-3], [4-6], [7-9], [10-12]
        var teamCountDict = teamCountByProject.ToDictionary(x => x.ProjectId, x => x.TeamCount);

        // teamId → quarter(1-4) → (demanda acumulada, títulos de proyectos)
        var demandByTeamQuarter =
            new Dictionary<int, Dictionary<int, (double Demand, List<string> Titles)>>();

        foreach (var team in teams)
            demandByTeamQuarter[team.Id] = new Dictionary<int, (double, List<string>)>
            {
                [1] = (0, []),
                [2] = (0, []),
                [3] = (0, []),
                [4] = (0, []),
            };

        foreach (var assignment in assignmentsByTeam)
        {
            if (!projectRhythm.TryGetValue(assignment.ProjectId, out var rhythm)) continue;
            if (!demandByTeamQuarter.TryGetValue(assignment.TeamId, out var quarters)) continue;

            var project = projects.First(p => p.Id == assignment.ProjectId);
            var startDate = project.StartDate!.Value;
            var endDateOpt = project.EndDate ?? project.DesiredDeploymentDate;

            double durationMonths;
            if (endDateOpt.HasValue)
            {
                var days = (endDateOpt.Value.ToDateTime(TimeOnly.MinValue)
                          - startDate.ToDateTime(TimeOnly.MinValue)).TotalDays;
                durationMonths = Math.Max(days / DaysPerMonth, 0.5);
            }
            else
            {
                durationMonths = DefaultDurationByComplexity[project.Complexity];
            }

            var projEnd = endDateOpt
                ?? DateOnly.FromDateTime(startDate.ToDateTime(TimeOnly.MinValue)
                   .AddDays(durationMonths * DaysPerMonth));

            var numTeams = teamCountDict.TryGetValue(assignment.ProjectId, out var tc) ? tc : 1;

            for (var q = 1; q <= 4; q++)
            {
                var overlapMonths = ComputeQuarterOverlapMonths(year, q, startDate, projEnd);
                if (overlapMonths <= 0) continue;

                var demand = rhythm.MonthlyRate * overlapMonths / numTeams;
                var (existingDemand, titles) = quarters[q];
                quarters[q] = (existingDemand + demand, titles);
                if (!titles.Contains(rhythm.Title))
                    titles.Add(rhythm.Title);
            }
        }

        // ── Construir resultado ───────────────────────────────────────────────
        var teamDtos = teams.Select(team =>
        {
            var memberCount = team.Members.Count;
            var capacityPerQuarter = memberCount * 3 * AvailabilityFactor;

            var quarters = Enumerable.Range(1, 4).Select(q =>
            {
                var (demand, titles) = demandByTeamQuarter.TryGetValue(team.Id, out var qDict)
                    ? qDict[q]
                    : (0.0, new List<string>());

                var roundedDemand   = Math.Round(demand, 2);
                var roundedCapacity = Math.Round(capacityPerQuarter, 2);

                int loadPercent;
                if (capacityPerQuarter == 0)
                    loadPercent = demand > 0 ? 999 : 0;
                else
                    loadPercent = (int)Math.Round(demand / capacityPerQuarter * 100);

                var level = loadPercent >= 999
                    ? "Red"
                    : loadPercent > 100
                        ? "Red"
                        : loadPercent >= 70
                            ? "Yellow"
                            : "Green";

                return new ForecastQuarterDto(
                    q,
                    roundedDemand,
                    roundedCapacity,
                    loadPercent,
                    level,
                    titles.OrderBy(t => t).ToList());
            }).ToList();

            return new ForecastTeamDto(team.Id, team.Name, memberCount, quarters);
        }).ToList();

        const string methodologyNote =
            "La demanda se estima repartiendo el esfuerzo total por complejidad " +
            "(VerySmall=1, Small=2, Medium=4, Large=16, VeryLarge=24 persona-mes) " +
            "a lo largo de la duración del proyecto (fechas reales o duración por defecto), " +
            "calculando el ritmo mensual y acumulando el solapamiento con cada trimestre; " +
            "la capacidad es nº miembros × 3 meses × 0,8 de disponibilidad.";

        return new CapacityForecastDto(year, methodologyNote, teamDtos);
    }

    /// <summary>
    /// Calcula los meses (con precisión fraccional de días/30.44) que el rango
    /// [projectStart, projectEnd] solapa con el trimestre <paramref name="quarter"/>
    /// del año <paramref name="year"/>.
    /// </summary>
    private static double ComputeQuarterOverlapMonths(
        int year, int quarter, DateOnly projectStart, DateOnly projectEnd)
    {
        var (qStartMonth, qEndMonth) = quarter switch
        {
            1 => (1, 3),
            2 => (4, 6),
            3 => (7, 9),
            _ => (10, 12),
        };

        var qStart = new DateOnly(year, qStartMonth, 1);
        var qEnd   = new DateOnly(year, qEndMonth, DateTime.DaysInMonth(year, qEndMonth));

        // Solapamiento
        var overlapStart = projectStart > qStart ? projectStart : qStart;
        var overlapEnd   = projectEnd   < qEnd   ? projectEnd   : qEnd;

        if (overlapEnd < overlapStart) return 0;

        var days = (overlapEnd.ToDateTime(TimeOnly.MinValue)
                  - overlapStart.ToDateTime(TimeOnly.MinValue)).TotalDays
                  + 1; // inclusivo

        return days / DaysPerMonth;
    }
}
