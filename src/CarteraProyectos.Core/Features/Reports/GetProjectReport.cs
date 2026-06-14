using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.Reports;

public record GetProjectReportQuery(int ProjectId) : IRequest<ProjectReportDto>;

public record ProjectReportDto(
    int ProjectId, string Title, string Status, string RequestingUnit,
    string? StartDate, string? EndDate,
    int TotalWorkItems, int DoneWorkItems,
    IReadOnlyList<EpicReportDto> Epics,
    IReadOnlyList<MilestoneDto> MilestonesReached,
    IReadOnlyList<MilestoneDto> MilestonesUpcoming,
    IReadOnlyList<SprintReportDto> Sprints);

public record EpicReportDto(int Id, string Title, int TotalWorkItems, int DoneWorkItems);

public record MilestoneDto(
    int Id, string Title, string Status,
    string? HitoDate, IReadOnlyList<string> Assignees);

public record SprintReportDto(
    int Id, string Name, string Status,
    int TotalWorkItems, int DoneWorkItems, int TotalPoints);

public sealed class GetProjectReportHandler(IAppDbContext db)
    : IRequestHandler<GetProjectReportQuery, ProjectReportDto>
{
    public async Task<ProjectReportDto> Handle(GetProjectReportQuery request, CancellationToken ct)
    {
        var project = await db.Projects.FindAsync([request.ProjectId], ct)
            ?? throw new KeyNotFoundException($"Project {request.ProjectId} not found.");

        var totalWi = await db.WorkItems.CountAsync(w => w.ProjectId == request.ProjectId, ct);
        var doneWi  = await db.WorkItems.CountAsync(w => w.ProjectId == request.ProjectId && w.Status == WorkItemStatus.Done, ct);

        var epics = await db.Epics
            .Where(e => e.ProjectId == request.ProjectId)
            .OrderBy(e => e.SortOrder).ThenBy(e => e.Id)
            .Select(e => new EpicReportDto(
                e.Id, e.Title,
                db.WorkItems.Count(w => w.EpicId == e.Id),
                db.WorkItems.Count(w => w.EpicId == e.Id && w.Status == WorkItemStatus.Done)))
            .ToListAsync(ct);

        var allMilestones = await db.WorkItems
            .Where(w => w.ProjectId == request.ProjectId && w.IsHito)
            .OrderBy(w => w.HitoDate).ThenBy(w => w.Id)
            .Select(w => new MilestoneDto(
                w.Id, w.Title, w.Status.ToString(),
                w.HitoDate != null ? w.HitoDate.Value.ToString() : null,
                w.Assignees.Select(a => a.Name).ToList()))
            .ToListAsync(ct);

        var sprints = await db.Sprints
            .Where(s => s.ProjectId == request.ProjectId)
            .OrderByDescending(s => s.StartDate).ThenByDescending(s => s.Id)
            .Select(s => new SprintReportDto(
                s.Id, s.Name, s.Status.ToString(),
                s.WorkItems.Count,
                s.WorkItems.Count(w => w.Status == WorkItemStatus.Done),
                s.WorkItems.Sum(w => w.EstimationPoints ?? 0)))
            .ToListAsync(ct);

        return new ProjectReportDto(
            project.Id, project.Title, project.Status.ToString(), project.RequestingUnit,
            project.StartDate?.ToString(), project.EndDate?.ToString(),
            totalWi, doneWi, epics,
            allMilestones.Where(m => m.Status == "Done").ToList(),
            allMilestones.Where(m => m.Status != "Done").ToList(),
            sprints);
    }
}
