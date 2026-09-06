using CarteraProyectos.Core.Common;
using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.Activity;

public record GetActivityFeedQuery(
    int? ProjectId = null,
    int? TeamId = null,
    int? PersonId = null,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<ActivityEventDto>>;

public record ActivityEventDto(
    string Type,
    DateTimeOffset OccurredAt,
    int ProjectId,
    string ProjectTitle,
    int ActorId,
    string ActorName,
    string Summary);

public sealed class GetActivityFeedHandler(IAppDbContext db)
    : IRequestHandler<GetActivityFeedQuery, PagedResult<ActivityEventDto>>
{
    private const int CommentSummaryMaxLength = 140;

    public async Task<PagedResult<ActivityEventDto>> Handle(GetActivityFeedQuery request, CancellationToken ct)
    {
        var pageSize = Math.Min(request.PageSize, 100);
        var page = Math.Max(request.Page, 1);
        var take = page * pageSize;

        var projectId = request.ProjectId;
        var teamId = request.TeamId;
        var personId = request.PersonId;

        // Proyectos que tienen el equipo asignado (para el filtro por equipo).
        IQueryable<int>? teamProjectIds = teamId.HasValue
            ? db.ProjectTeamAssignments.Where(a => a.TeamId == teamId.Value).Select(a => a.ProjectId)
            : null;

        // 1. Cambios de estado de proyecto (excluida la entrada de creación con FromStatus == null).
        var statusChangesQuery = db.ProjectStatusHistories
            .Where(h => h.FromStatus != null);
        if (projectId.HasValue)
            statusChangesQuery = statusChangesQuery.Where(h => h.ProjectId == projectId.Value);
        if (teamProjectIds is not null)
            statusChangesQuery = statusChangesQuery.Where(h => teamProjectIds.Contains(h.ProjectId));
        if (personId.HasValue)
            statusChangesQuery = statusChangesQuery.Where(h => h.ChangedById == personId.Value);

        var statusChanges = await statusChangesQuery
            .OrderByDescending(h => h.ChangedAt)
            .Take(take)
            .Select(h => new
            {
                h.ChangedAt,
                h.ProjectId,
                ProjectTitle = h.Project!.Title,
                ActorId = h.ChangedById,
                ActorName = h.ChangedBy!.Name,
                h.FromStatus,
                h.ToStatus
            })
            .ToListAsync(ct);

        // 2. Tareas creadas (WorkItemStatusHistory con FromStatus == null).
        var createdQuery = db.WorkItemStatusHistories
            .Where(h => h.FromStatus == null);
        if (projectId.HasValue)
            createdQuery = createdQuery.Where(h => h.WorkItem!.ProjectId == projectId.Value);
        if (teamProjectIds is not null)
            createdQuery = createdQuery.Where(h => teamProjectIds.Contains(h.WorkItem!.ProjectId));
        if (personId.HasValue)
            createdQuery = createdQuery.Where(h => h.ChangedById == personId.Value);

        var created = await createdQuery
            .OrderByDescending(h => h.ChangedAt)
            .Take(take)
            .Select(h => new
            {
                h.ChangedAt,
                ProjectId = h.WorkItem!.ProjectId,
                ProjectTitle = h.WorkItem!.Project!.Title,
                ActorId = h.ChangedById,
                ActorName = h.ChangedBy!.Name,
                Title = h.WorkItem!.Title
            })
            .ToListAsync(ct);

        // 3. Tareas completadas (WorkItemStatusHistory con ToStatus == Done).
        var completedQuery = db.WorkItemStatusHistories
            .Where(h => h.ToStatus == WorkItemStatus.Done);
        if (projectId.HasValue)
            completedQuery = completedQuery.Where(h => h.WorkItem!.ProjectId == projectId.Value);
        if (teamProjectIds is not null)
            completedQuery = completedQuery.Where(h => teamProjectIds.Contains(h.WorkItem!.ProjectId));
        if (personId.HasValue)
            completedQuery = completedQuery.Where(h => h.ChangedById == personId.Value);

        var completed = await completedQuery
            .OrderByDescending(h => h.ChangedAt)
            .Take(take)
            .Select(h => new
            {
                h.ChangedAt,
                ProjectId = h.WorkItem!.ProjectId,
                ProjectTitle = h.WorkItem!.Project!.Title,
                ActorId = h.ChangedById,
                ActorName = h.ChangedBy!.Name,
                Title = h.WorkItem!.Title
            })
            .ToListAsync(ct);

        // 4. Comentarios (el proyecto se obtiene vía WorkItem.ProjectId).
        var commentsQuery = db.Comments.AsQueryable();
        if (projectId.HasValue)
            commentsQuery = commentsQuery.Where(c => c.WorkItem!.ProjectId == projectId.Value);
        if (teamProjectIds is not null)
            commentsQuery = commentsQuery.Where(c => teamProjectIds.Contains(c.WorkItem!.ProjectId));
        if (personId.HasValue)
            commentsQuery = commentsQuery.Where(c => c.AuthorId == personId.Value);

        var comments = await commentsQuery
            .OrderByDescending(c => c.CreatedAt)
            .Take(take)
            .Select(c => new
            {
                c.CreatedAt,
                ProjectId = c.WorkItem!.ProjectId,
                ProjectTitle = c.WorkItem!.Project!.Title,
                ActorId = c.AuthorId,
                ActorName = c.Author!.Name,
                c.Text
            })
            .ToListAsync(ct);

        // 5. Actualizaciones semanales de avance.
        var weeklyQuery = db.ProjectWeeklyUpdates.AsQueryable();
        if (projectId.HasValue)
            weeklyQuery = weeklyQuery.Where(u => u.ProjectId == projectId.Value);
        if (teamProjectIds is not null)
            weeklyQuery = weeklyQuery.Where(u => teamProjectIds.Contains(u.ProjectId));
        if (personId.HasValue)
            weeklyQuery = weeklyQuery.Where(u => u.AuthorId == personId.Value);

        var weekly = await weeklyQuery
            .OrderByDescending(u => u.CreatedAt)
            .Take(take)
            .Select(u => new
            {
                u.CreatedAt,
                u.ProjectId,
                ProjectTitle = u.Project!.Title,
                ActorId = u.AuthorId,
                ActorName = u.Author!.Name,
                u.Summary
            })
            .ToListAsync(ct);

        // Proyectar cada fuente al DTO común, normalizando la fecha (UTC por convención) a DateTimeOffset.
        var events = new List<ActivityEventDto>(
            statusChanges.Count + created.Count + completed.Count + comments.Count + weekly.Count);

        events.AddRange(statusChanges.Select(h => new ActivityEventDto(
            "ProjectStatusChanged",
            ToUtcOffset(h.ChangedAt),
            h.ProjectId,
            h.ProjectTitle,
            h.ActorId,
            h.ActorName,
            $"De {h.FromStatus} a {h.ToStatus}")));

        events.AddRange(created.Select(h => new ActivityEventDto(
            "WorkItemCreated",
            ToUtcOffset(h.ChangedAt),
            h.ProjectId,
            h.ProjectTitle,
            h.ActorId,
            h.ActorName,
            h.Title)));

        events.AddRange(completed.Select(h => new ActivityEventDto(
            "WorkItemCompleted",
            ToUtcOffset(h.ChangedAt),
            h.ProjectId,
            h.ProjectTitle,
            h.ActorId,
            h.ActorName,
            h.Title)));

        events.AddRange(comments.Select(c => new ActivityEventDto(
            "CommentAdded",
            ToUtcOffset(c.CreatedAt),
            c.ProjectId,
            c.ProjectTitle,
            c.ActorId,
            c.ActorName,
            Truncate(c.Text, CommentSummaryMaxLength))));

        events.AddRange(weekly.Select(u => new ActivityEventDto(
            "WeeklyUpdateRegistered",
            u.CreatedAt,
            u.ProjectId,
            u.ProjectTitle,
            u.ActorId,
            u.ActorName,
            u.Summary)));

        // Merge en memoria: ordenar por fecha descendente y aplicar la página pedida.
        var items = events
            .OrderByDescending(e => e.OccurredAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        // Total: 5 CountAsync con los mismos filtros, sin traer filas.
        var total =
            await statusChangesQuery.CountAsync(ct) +
            await createdQuery.CountAsync(ct) +
            await completedQuery.CountAsync(ct) +
            await commentsQuery.CountAsync(ct) +
            await weeklyQuery.CountAsync(ct);

        return new PagedResult<ActivityEventDto>(items, total, page, pageSize);
    }

    private static DateTimeOffset ToUtcOffset(DateTime value)
        => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));

    private static string Truncate(string text, int maxLength)
        => text.Length <= maxLength ? text : text[..maxLength] + "…";
}
