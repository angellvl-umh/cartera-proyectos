using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.Reports;

public record GetPortfolioQuery(int? Year = null, string? Status = null) : IRequest<PortfolioDto>;

public record PortfolioDto(
    IReadOnlyList<PortfolioProjectDto> Projects,
    PortfolioStatsDto Stats,
    IReadOnlyList<int> AvailableYears);

public record PortfolioProjectDto(
    int Id, string Title, string Status, string? RequestingUnit, string Complexity,
    int? PortfolioYear, string? StartDate, string? EndDate,
    string? PrimaryTeamName,
    int TotalWorkItems, int DoneWorkItems,
    int TotalMilestones, int ReachedMilestones,
    int ActiveSprintCount);

public record PortfolioStatsDto(
    int Total,
    int Stopped,
    int PlanningWithClient,
    int PlanningSprint,
    int InSprint,
    int DevelopmentOutsideSprint,
    int InTesting,
    int Completed,
    int PostponedByClient);

public sealed class GetPortfolioHandler(IAppDbContext db)
    : IRequestHandler<GetPortfolioQuery, PortfolioDto>
{
    public async Task<PortfolioDto> Handle(GetPortfolioQuery request, CancellationToken ct)
    {
        var availableYears = await db.Projects
            .Where(p => p.PortfolioYear != null)
            .Select(p => p.PortfolioYear!.Value)
            .Distinct()
            .OrderByDescending(y => y)
            .ToListAsync(ct);

        var query = db.Projects.AsQueryable();

        if (request.Year.HasValue)
            query = query.Where(p => p.PortfolioYear == request.Year.Value);

        if (!string.IsNullOrEmpty(request.Status) &&
            Enum.TryParse<ProjectStatus>(request.Status, out var statusEnum))
            query = query.Where(p => p.Status == statusEnum);

        var allProjects = await query
            .OrderByDescending(p => p.PortfolioYear).ThenBy(p => p.Title)
            .ToListAsync(ct);

        var projectIds = allProjects.Select(p => p.Id).ToList();

        var primaryTeams = await db.ProjectTeamAssignments
            .Where(a => projectIds.Contains(a.ProjectId) && a.IsPrimary)
            .Select(a => new { a.ProjectId, a.Team.Name })
            .ToListAsync(ct);

        var workItemCounts = await db.WorkItems
            .Where(w => projectIds.Contains(w.ProjectId))
            .GroupBy(w => new { w.ProjectId, IsDone = w.Status == WorkItemStatus.Done })
            .Select(g => new { g.Key.ProjectId, g.Key.IsDone, Count = g.Count() })
            .ToListAsync(ct);

        var milestoneCounts = await db.WorkItems
            .Where(w => projectIds.Contains(w.ProjectId) && w.IsHito)
            .GroupBy(w => new { w.ProjectId, IsReached = w.Status == WorkItemStatus.Done })
            .Select(g => new { g.Key.ProjectId, g.Key.IsReached, Count = g.Count() })
            .ToListAsync(ct);

        var activeSprintCounts = await db.Sprints
            .Where(s => projectIds.Contains(s.ProjectId) && s.Status == SprintStatus.Active)
            .GroupBy(s => s.ProjectId)
            .Select(g => new { ProjectId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var projects = allProjects.Select(p =>
        {
            var teamName = primaryTeams.FirstOrDefault(t => t.ProjectId == p.Id)?.Name;
            var total = workItemCounts.Where(w => w.ProjectId == p.Id).Sum(w => w.Count);
            var done  = workItemCounts.Where(w => w.ProjectId == p.Id && w.IsDone).Sum(w => w.Count);
            var totalM   = milestoneCounts.Where(m => m.ProjectId == p.Id).Sum(m => m.Count);
            var reachedM = milestoneCounts.Where(m => m.ProjectId == p.Id && m.IsReached).Sum(m => m.Count);
            var activeSprints = activeSprintCounts.FirstOrDefault(s => s.ProjectId == p.Id)?.Count ?? 0;

            return new PortfolioProjectDto(
                p.Id, p.Title, p.Status.ToString(), p.RequestingUnit, p.Complexity.ToString(),
                p.PortfolioYear, p.StartDate?.ToString(), p.EndDate?.ToString(),
                teamName, total, done, totalM, reachedM, activeSprints);
        }).ToList();

        var stats = new PortfolioStatsDto(
            Total:                  allProjects.Count,
            Stopped:                allProjects.Count(p => p.Status == ProjectStatus.Stopped),
            PlanningWithClient:     allProjects.Count(p => p.Status == ProjectStatus.PlanningWithClient),
            PlanningSprint:         allProjects.Count(p => p.Status == ProjectStatus.PlanningSprint),
            InSprint:               allProjects.Count(p => p.Status == ProjectStatus.InSprint),
            DevelopmentOutsideSprint: allProjects.Count(p => p.Status == ProjectStatus.DevelopmentOutsideSprint),
            InTesting:              allProjects.Count(p => p.Status == ProjectStatus.InTesting),
            Completed:              allProjects.Count(p => p.Status == ProjectStatus.Completed),
            PostponedByClient:      allProjects.Count(p => p.Status == ProjectStatus.PostponedByClient));

        return new PortfolioDto(projects, stats, availableYears);
    }
}
