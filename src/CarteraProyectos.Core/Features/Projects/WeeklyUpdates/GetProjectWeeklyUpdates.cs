using CarteraProyectos.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.Projects.WeeklyUpdates;

public record GetProjectWeeklyUpdatesQuery(int ProjectId) : IRequest<List<ProjectWeeklyUpdateDto>>;

public record ProjectWeeklyUpdateDto(int Id, int AuthorId, string AuthorName, DateOnly WeekOf, string Summary, string HealthStatus, DateTimeOffset UpdatedAt);

public sealed class GetProjectWeeklyUpdatesHandler(IAppDbContext db) : IRequestHandler<GetProjectWeeklyUpdatesQuery, List<ProjectWeeklyUpdateDto>>
{
    public async Task<List<ProjectWeeklyUpdateDto>> Handle(GetProjectWeeklyUpdatesQuery request, CancellationToken cancellationToken)
    {
        if (!await db.Projects.AnyAsync(p => p.Id == request.ProjectId, cancellationToken))
            throw new KeyNotFoundException($"Proyecto con Id {request.ProjectId} no encontrado.");

        return await db.ProjectWeeklyUpdates
            .Include(u => u.Author)
            .Where(u => u.ProjectId == request.ProjectId)
            .OrderByDescending(u => u.WeekOf)
            .Select(u => new ProjectWeeklyUpdateDto(
                u.Id, u.AuthorId,
                u.Author!.Name, u.WeekOf, u.Summary,
                u.HealthStatus.ToString(), u.UpdatedAt))
            .ToListAsync(cancellationToken);
    }
}
