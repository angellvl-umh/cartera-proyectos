using CarteraProyectos.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.WorkItems;

public record GetWorkItemStatusHistoryQuery(int ProjectId, int WorkItemId)
    : IRequest<IReadOnlyList<WorkItemStatusHistoryDto>>;

public record WorkItemStatusHistoryDto(
    int Id, string? FromStatus, string ToStatus,
    int ChangedById, string ChangedByName, DateTime ChangedAt);

public sealed class GetWorkItemStatusHistoryHandler(IAppDbContext db)
    : IRequestHandler<GetWorkItemStatusHistoryQuery, IReadOnlyList<WorkItemStatusHistoryDto>>
{
    public async Task<IReadOnlyList<WorkItemStatusHistoryDto>> Handle(
        GetWorkItemStatusHistoryQuery request, CancellationToken cancellationToken)
    {
        var workItemExists = await db.WorkItems
            .AnyAsync(w => w.Id == request.WorkItemId && w.ProjectId == request.ProjectId, cancellationToken);
        if (!workItemExists)
            throw new KeyNotFoundException($"Tarea {request.WorkItemId} no encontrada en el proyecto.");

        return await db.WorkItemStatusHistories
            .Where(h => h.WorkItemId == request.WorkItemId)
            .OrderBy(h => h.ChangedAt)
            .Select(h => new WorkItemStatusHistoryDto(
                h.Id,
                h.FromStatus.HasValue ? h.FromStatus.Value.ToString() : null,
                h.ToStatus.ToString(),
                h.ChangedById,
                h.ChangedBy!.Name,
                h.ChangedAt))
            .ToListAsync(cancellationToken);
    }
}
