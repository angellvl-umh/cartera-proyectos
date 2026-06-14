using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.Reports;

public record GetMyWorkItemsQuery(
    int PersonId,
    WorkItemStatus? Status = null,
    int Page = 1,
    int PageSize = 50) : IRequest<MyWorkItemsDto>;

public record MyWorkItemsDto(
    IReadOnlyList<MyWorkItemDto> Items,
    int Total,
    WorkItemCountsDto Counts);

public record MyWorkItemDto(
    int Id, int ProjectId, string ProjectTitle,
    int? EpicId, string? EpicTitle,
    int? SprintId, string? SprintName,
    string Title, string Status, string Priority,
    int? EstimationHours, int? EstimationPoints,
    bool IsHito, string? HitoDate, string? DueDate);

public record WorkItemCountsDto(
    int Total, int Backlog, int ToDo, int InProgress, int Blocked, int Done);

public sealed class GetMyWorkItemsHandler(IAppDbContext db)
    : IRequestHandler<GetMyWorkItemsQuery, MyWorkItemsDto>
{
    public async Task<MyWorkItemsDto> Handle(GetMyWorkItemsQuery request, CancellationToken ct)
    {
        var pageSize = Math.Min(request.PageSize, 100);
        var page     = Math.Max(request.Page, 1);

        var baseQuery = db.WorkItems.Where(w => w.Assignees.Any(a => a.Id == request.PersonId));

        var counts = new WorkItemCountsDto(
            Total:      await baseQuery.CountAsync(ct),
            Backlog:    await baseQuery.CountAsync(w => w.Status == WorkItemStatus.Backlog, ct),
            ToDo:       await baseQuery.CountAsync(w => w.Status == WorkItemStatus.ToDo, ct),
            InProgress: await baseQuery.CountAsync(w => w.Status == WorkItemStatus.InProgress, ct),
            Blocked:    await baseQuery.CountAsync(w => w.Status == WorkItemStatus.Blocked, ct),
            Done:       await baseQuery.CountAsync(w => w.Status == WorkItemStatus.Done, ct));

        if (request.Status.HasValue)
            baseQuery = baseQuery.Where(w => w.Status == request.Status.Value);

        var total = await baseQuery.CountAsync(ct);

        var statusOrder = new[] {
            WorkItemStatus.InProgress, WorkItemStatus.Blocked, WorkItemStatus.ToDo,
            WorkItemStatus.Backlog, WorkItemStatus.Done
        };

        var items = await baseQuery
            .OrderBy(w => w.Status == WorkItemStatus.Done ? 1 : 0)
            .ThenBy(w => w.Priority == WorkItemPriority.Critical ? 0
                       : w.Priority == WorkItemPriority.High ? 1
                       : w.Priority == WorkItemPriority.Medium ? 2 : 3)
            .ThenBy(w => w.DueDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(w => new MyWorkItemDto(
                w.Id, w.ProjectId,
                w.Project != null ? w.Project.Title : string.Empty,
                w.EpicId,
                w.Epic != null ? w.Epic.Title : null,
                w.SprintId,
                w.Sprint != null ? w.Sprint.Name : null,
                w.Title, w.Status.ToString(), w.Priority.ToString(),
                w.EstimationHours, w.EstimationPoints,
                w.IsHito,
                w.HitoDate != null ? w.HitoDate.Value.ToString() : null,
                w.DueDate != null ? w.DueDate.Value.ToString() : null))
            .ToListAsync(ct);

        return new MyWorkItemsDto(items, total, counts);
    }
}
