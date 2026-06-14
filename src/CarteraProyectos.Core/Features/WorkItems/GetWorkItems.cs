using CarteraProyectos.Core.Common;
using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.WorkItems;

public record GetWorkItemsQuery(
    int ProjectId,
    int? EpicId = null,
    WorkItemStatus? Status = null,
    int Page = 1,
    int PageSize = 50,
    int? SprintId = null,
    bool BacklogOnly = false) : IRequest<PagedResult<WorkItemDto>>;

public record AssigneeDto(int Id, string Name);

public record WorkItemDto(
    int Id, int ProjectId, int? EpicId, string? EpicTitle,
    int? SprintId, string? SprintName,
    string Title, string? Description, string Status, string Priority,
    List<AssigneeDto> Assignees,
    int SortOrder, int? EstimationHours, int? EstimationPoints,
    bool IsHito, DateOnly? HitoDate, DateOnly? DueDate);

public sealed class GetWorkItemsHandler(IAppDbContext db) : IRequestHandler<GetWorkItemsQuery, PagedResult<WorkItemDto>>
{
    public async Task<PagedResult<WorkItemDto>> Handle(GetWorkItemsQuery request, CancellationToken cancellationToken)
    {
        var pageSize = Math.Min(request.PageSize, 100);
        var page = Math.Max(request.Page, 1);

        var query = db.WorkItems
            .Where(w => w.ProjectId == request.ProjectId);

        if (request.EpicId.HasValue)
            query = query.Where(w => w.EpicId == request.EpicId.Value);

        if (request.Status.HasValue)
            query = query.Where(w => w.Status == request.Status.Value);

        if (request.SprintId.HasValue)
            query = query.Where(w => w.SprintId == request.SprintId.Value);
        else if (request.BacklogOnly)
            query = query.Where(w => w.SprintId == null);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(w => w.SortOrder).ThenBy(w => w.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(w => new WorkItemDto(
                w.Id, w.ProjectId, w.EpicId,
                w.Epic != null ? w.Epic.Title : null,
                w.SprintId,
                w.Sprint != null ? w.Sprint.Name : null,
                w.Title, w.Description,
                w.Status.ToString(), w.Priority.ToString(),
                w.Assignees.Select(a => new AssigneeDto(a.Id, a.Name)).ToList(),
                w.SortOrder, w.EstimationHours, w.EstimationPoints,
                w.IsHito, w.HitoDate, w.DueDate))
            .ToListAsync(cancellationToken);

        return new PagedResult<WorkItemDto>(items, total, page, pageSize);
    }
}
