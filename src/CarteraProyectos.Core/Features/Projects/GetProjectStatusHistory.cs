using CarteraProyectos.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.Projects;

public record GetProjectStatusHistoryQuery(int ProjectId)
    : IRequest<IReadOnlyList<ProjectStatusHistoryDto>>;

public record ProjectStatusHistoryDto(
    int Id, string? FromStatus, string ToStatus,
    int ChangedById, string ChangedByName, DateTime ChangedAt);

public sealed class GetProjectStatusHistoryHandler(IAppDbContext db)
    : IRequestHandler<GetProjectStatusHistoryQuery, IReadOnlyList<ProjectStatusHistoryDto>>
{
    public async Task<IReadOnlyList<ProjectStatusHistoryDto>> Handle(
        GetProjectStatusHistoryQuery request, CancellationToken cancellationToken)
    {
        var projectExists = await db.Projects
            .AnyAsync(p => p.Id == request.ProjectId, cancellationToken);
        if (!projectExists)
            throw new KeyNotFoundException($"Proyecto {request.ProjectId} no encontrado.");

        return await db.ProjectStatusHistories
            .Where(h => h.ProjectId == request.ProjectId)
            .OrderBy(h => h.ChangedAt)
            .Select(h => new ProjectStatusHistoryDto(
                h.Id,
                h.FromStatus.HasValue ? h.FromStatus.Value.ToString() : null,
                h.ToStatus.ToString(),
                h.ChangedById,
                h.ChangedBy!.Name,
                h.ChangedAt))
            .ToListAsync(cancellationToken);
    }
}
