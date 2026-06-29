using CarteraProyectos.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.Sprints;

public record GetSprintStatusHistoryQuery(int ProjectId, int SprintId)
    : IRequest<IReadOnlyList<SprintStatusHistoryDto>>;

public record SprintStatusHistoryDto(
    int Id, string? FromStatus, string ToStatus,
    int ChangedById, string ChangedByName, DateTime ChangedAt);

public sealed class GetSprintStatusHistoryHandler(IAppDbContext db)
    : IRequestHandler<GetSprintStatusHistoryQuery, IReadOnlyList<SprintStatusHistoryDto>>
{
    public async Task<IReadOnlyList<SprintStatusHistoryDto>> Handle(
        GetSprintStatusHistoryQuery request, CancellationToken cancellationToken)
    {
        var sprintExists = await db.Sprints
            .AnyAsync(s => s.Id == request.SprintId && s.ProjectId == request.ProjectId, cancellationToken);
        if (!sprintExists)
            throw new KeyNotFoundException($"Sprint {request.SprintId} no encontrado en el proyecto.");

        return await db.SprintStatusHistories
            .Where(h => h.SprintId == request.SprintId)
            .OrderBy(h => h.ChangedAt)
            .Select(h => new SprintStatusHistoryDto(
                h.Id,
                h.FromStatus.HasValue ? h.FromStatus.Value.ToString() : null,
                h.ToStatus.ToString(),
                h.ChangedById,
                h.ChangedBy!.Name,
                h.ChangedAt))
            .ToListAsync(cancellationToken);
    }
}
