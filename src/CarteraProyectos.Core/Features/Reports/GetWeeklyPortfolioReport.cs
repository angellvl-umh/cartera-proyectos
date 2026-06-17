using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.Reports;

public record GetWeeklyPortfolioReportQuery(int? Year = null, int? TeamId = null, string? SiptGroup = null) : IRequest<WeeklyPortfolioReportDto>;

public record WeeklyPortfolioReportDto(
    IReadOnlyList<WeeklyPortfolioProjectDto> AtRiskProjects,
    IReadOnlyList<WeeklyPortfolioProjectDto> OtherProjects);

public record WeeklyPortfolioProjectDto(
    int ProjectId, string Title, string? PrimaryTeamName, string Status,
    bool IsAtRisk, string? LatestSummary, string? LatestHealthStatus,
    string? LatestAuthorName, string? LatestUpdateDate, bool HasUpdateThisWeek, int? PortfolioYear = null);

public sealed class GetWeeklyPortfolioReportHandler(IAppDbContext db)
    : IRequestHandler<GetWeeklyPortfolioReportQuery, WeeklyPortfolioReportDto>
{
    public async Task<WeeklyPortfolioReportDto> Handle(GetWeeklyPortfolioReportQuery request, CancellationToken ct)
    {
        var thisWeekMonday = GetMondayOfWeek(DateTime.UtcNow);

        // Proyectos en curso: Status NOT IN (Stopped, Completed, PostponedByClient)
        var query = db.Projects
            .Where(p => p.Status != ProjectStatus.Stopped &&
                        p.Status != ProjectStatus.Completed &&
                        p.Status != ProjectStatus.PostponedByClient);

        // Filtro por Year
        if (request.Year.HasValue)
            query = query.Where(p => p.PortfolioYear == request.Year.Value);

        // Filtro por TeamId
        if (request.TeamId.HasValue)
            query = query.Where(p => p.Teams.Any(t => t.TeamId == request.TeamId.Value));

        // Filtro por SiptGroup
        if (!string.IsNullOrEmpty(request.SiptGroup) &&
            Enum.TryParse<SiptGroup>(request.SiptGroup, out var siptGroupEnum))
            query = query.Where(p => p.SiptGroup == siptGroupEnum);

        var projects = await query.OrderBy(p => p.Title).ToListAsync(ct);
        var projectIds = projects.Select(p => p.Id).ToList();

        // Obtener equipos primarios
        var primaryTeams = await db.ProjectTeamAssignments
            .Where(a => projectIds.Contains(a.ProjectId) && a.IsPrimary)
            .Select(a => new { a.ProjectId, a.Team.Name })
            .ToListAsync(ct);

        // Obtener actualizaciones más recientes de cada proyecto
        var latestUpdates = await db.ProjectWeeklyUpdates
            .Where(u => projectIds.Contains(u.ProjectId))
            .GroupBy(u => u.ProjectId)
            .Select(g => new
            {
                ProjectId = g.Key,
                Update = g.OrderByDescending(u => u.WeekOf).ThenByDescending(u => u.CreatedAt).FirstOrDefault()
            })
            .ToListAsync(ct);

        var updatesByProject = latestUpdates
            .Where(x => x.Update != null)
            .ToDictionary(x => x.ProjectId, x => x.Update!);

        // Obtener autores de las updates
        var authorIds = updatesByProject.Values.Select(u => u.AuthorId).Distinct().ToList();
        var authors = await db.Persons
            .Where(p => authorIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, ct);

        // Mapear DTOs
        var projectDtos = projects.Select(p =>
        {
            var primaryTeamName = primaryTeams.FirstOrDefault(t => t.ProjectId == p.Id)?.Name;
            var hasUpdate = updatesByProject.TryGetValue(p.Id, out var update);
            var hasUpdateThisWeek = hasUpdate && update is not null && update.WeekOf == thisWeekMonday;
            var isAtRisk = !hasUpdateThisWeek || (hasUpdate && update is not null && (update.HealthStatus == ProjectHealthStatus.AtRisk || update.HealthStatus == ProjectHealthStatus.Blocked));
            var authorName = hasUpdate && update is not null && authors.TryGetValue(update.AuthorId, out var author) ? author.Name : null;

            return new WeeklyPortfolioProjectDto(
                p.Id,
                p.Title,
                primaryTeamName,
                p.Status.ToString(),
                isAtRisk,
                update?.Summary,
                update?.HealthStatus.ToString(),
                authorName,
                update?.UpdatedAt.ToString("yyyy-MM-dd"),
                hasUpdateThisWeek,
                p.PortfolioYear
            );
        }).ToList();

        var atRiskProjects = projectDtos.Where(p => p.IsAtRisk).OrderBy(p => p.Title).ToList();
        var otherProjects = projectDtos.Where(p => !p.IsAtRisk).OrderBy(p => p.Title).ToList();

        return new WeeklyPortfolioReportDto(atRiskProjects, otherProjects);
    }

    private static DateOnly GetMondayOfWeek(DateTime date)
    {
        var dateOnly = DateOnly.FromDateTime(date);
        var daysOfWeek = (int)dateOnly.DayOfWeek;
        var daysToMonday = daysOfWeek == 0 ? 6 : daysOfWeek - 1;
        return dateOnly.AddDays(-daysToMonday);
    }
}
