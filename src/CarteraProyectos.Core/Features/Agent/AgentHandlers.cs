using CarteraProyectos.Core.Common;
using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Features.WorkItems;
using CarteraProyectos.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.Agent;

// ─── DTOs ────────────────────────────────────────────────────────────────────

public record AgentProjectSummaryDto(
    int Id, string Title, string Status, string? RequestingUnit,
    string? PrimaryTeamName,
    int TotalTasks, int DoneTasks, int ActiveSprints);

public record AgentProjectDetailDto(
    int Id, string Title, string Status, string? RequestingUnit,
    string? StartDate, string? EndDate,
    int TotalTasks, int DoneTasks,
    IReadOnlyList<AgentSprintSummaryDto> Sprints,
    IReadOnlyList<AgentTaskSummaryDto> PendingTasks);

public record AgentSprintSummaryDto(
    int Id, string Name, string Status, string? Goal,
    string? EndDate, int TotalTasks, int DoneTasks);

public record AgentTaskSummaryDto(
    int Id, string Title, string Status, string Priority,
    string? AssignedTo, int? EstimationHours, int? EstimationPoints,
    string? DueDate);

public record AgentMyTasksDto(
    string UserName, string Role,
    IReadOnlyList<AgentTaskSummaryDto> ActiveTasks,
    IReadOnlyList<AgentTaskSummaryDto> BacklogTasks,
    int DoneTasks);

public record AgentCapacityDto(
    IReadOnlyList<AgentTeamCapacityDto> Teams);

public record AgentTeamCapacityDto(
    int TeamId, string TeamName, int ActiveProjectCount,
    IReadOnlyList<AgentMemberCapacityDto> Members);

public record AgentMemberCapacityDto(
    string Name, string Role, int ActiveTasks, int PendingTasks, string LoadLevel);

public record AgentSearchResultDto(
    int Id, int ProjectId, string ProjectTitle,
    string Title, string Status, string Priority,
    double Score);

public record AgentReindexResultDto(int Indexed, int Failed);

// ─── Queries / Commands ───────────────────────────────────────────────────────

public record AgentGetMyTasksQuery(int PersonId) : IRequest<AgentMyTasksDto>;
public record AgentGetProjectsQuery(int PersonId, string? Status = null) : IRequest<IReadOnlyList<AgentProjectSummaryDto>>;
public record AgentGetProjectDetailQuery(int ProjectId) : IRequest<AgentProjectDetailDto>;
public record AgentGetCapacityQuery : IRequest<AgentCapacityDto>;
public record AgentSearchTasksQuery(string Q, int PersonId, int TopN = 5) : IRequest<IReadOnlyList<AgentSearchResultDto>>;
public record AgentReindexCommand : IRequest<AgentReindexResultDto>;

public record AgentCreateTaskCommand(
    int PersonId, int ProjectId, string Title,
    string? Description, string Priority,
    int? EpicId, int? SprintId,
    bool AssignToSelf) : IRequest<int>, IAgentAuditable
{
    public int RequestingPersonId => PersonId;
}

public record AgentUpdateTaskStatusCommand(
    int PersonId, int WorkItemId, string NewStatus) : IRequest, IAgentAuditable
{
    public int RequestingPersonId => PersonId;
}

public record AgentAddCommentCommand(
    int PersonId, int WorkItemId, string Text) : IRequest, IAgentAuditable
{
    public int RequestingPersonId => PersonId;
}

public record AgentAddProjectNoteCommand(
    int PersonId, int ProjectId, string Text) : IRequest<int>, IAgentAuditable
{
    public int RequestingPersonId => PersonId;
}

// ─── Handlers ────────────────────────────────────────────────────────────────

public sealed class AgentGetMyTasksHandler(IMyTasksReadService service)
    : IRequestHandler<AgentGetMyTasksQuery, AgentMyTasksDto>
{
    public Task<AgentMyTasksDto> Handle(AgentGetMyTasksQuery request, CancellationToken ct)
        => service.GetAsync(request.PersonId, ct);
}

public sealed class AgentGetProjectsHandler(IProjectsReadService service)
    : IRequestHandler<AgentGetProjectsQuery, IReadOnlyList<AgentProjectSummaryDto>>
{
    public Task<IReadOnlyList<AgentProjectSummaryDto>> Handle(AgentGetProjectsQuery request, CancellationToken ct)
        => service.GetAsync(request.PersonId, request.Status, ct);
}

public sealed class AgentGetProjectDetailHandler(IAppDbContext db)
    : IRequestHandler<AgentGetProjectDetailQuery, AgentProjectDetailDto>
{
    public async Task<AgentProjectDetailDto> Handle(AgentGetProjectDetailQuery request, CancellationToken ct)
    {
        var project = await db.Projects.FindAsync([request.ProjectId], ct)
            ?? throw new KeyNotFoundException($"Proyecto {request.ProjectId} no encontrado.");

        var sprints = await db.Sprints
            .Where(s => s.ProjectId == request.ProjectId)
            .OrderByDescending(s => s.StartDate)
            .Select(s => new AgentSprintSummaryDto(
                s.Id, s.Name, s.Status.ToString(), s.Goal,
                s.EndDate != null ? s.EndDate.Value.ToString() : null,
                s.WorkItems.Count,
                s.WorkItems.Count(w => w.Status == WorkItemStatus.Done)))
            .ToListAsync(ct);

        var pendingTasks = await db.WorkItems
            .Where(w => w.ProjectId == request.ProjectId && w.Status != WorkItemStatus.Done && w.Status != WorkItemStatus.Discarded)
            .OrderBy(w => w.Priority == WorkItemPriority.Critical ? 0 : w.Priority == WorkItemPriority.High ? 1 : 2)
            .Take(20)
            .Select(w => new AgentTaskSummaryDto(
                w.Id, w.Title, w.Status.ToString(), w.Priority.ToString(),
                w.Assignees.Select(a => a.Name).FirstOrDefault(),
                w.EstimationHours, w.EstimationPoints,
                w.DueDate != null ? w.DueDate.Value.ToString() : null))
            .ToListAsync(ct);

        var total = await db.WorkItems.CountAsync(w => w.ProjectId == request.ProjectId, ct);
        var done  = await db.WorkItems.CountAsync(w => w.ProjectId == request.ProjectId && w.Status == WorkItemStatus.Done, ct);

        return new AgentProjectDetailDto(
            project.Id, project.Title, project.Status.ToString(), project.RequestingUnit,
            project.StartDate?.ToString(), project.EndDate?.ToString(),
            total, done, sprints, pendingTasks);
    }
}

public sealed class AgentGetCapacityHandler(ICapacityReadService service)
    : IRequestHandler<AgentGetCapacityQuery, AgentCapacityDto>
{
    public Task<AgentCapacityDto> Handle(AgentGetCapacityQuery request, CancellationToken ct)
        => service.GetAsync(ct);
}

public sealed class AgentSearchTasksHandler(IAppDbContext db, IEmbeddingService embedding)
    : IRequestHandler<AgentSearchTasksQuery, IReadOnlyList<AgentSearchResultDto>>
{
    public async Task<IReadOnlyList<AgentSearchResultDto>> Handle(AgentSearchTasksQuery request, CancellationToken ct)
    {
        var queryEmbedding = await embedding.GetEmbeddingAsync(request.Q, ct);

        var allEmbeddings = await db.WorkItemEmbeddings
            .Select(e => new { e.WorkItemId, e.Embedding })
            .ToListAsync(ct);

        if (allEmbeddings.Count == 0)
        {
            // Fallback: text search when no embeddings indexed
            var q = request.Q.ToLower();
            return await db.WorkItems
                .Where(w => w.Title.ToLower().Contains(q) || (w.Description != null && w.Description.ToLower().Contains(q)))
                .Take(request.TopN)
                .Select(w => new AgentSearchResultDto(
                    w.Id, w.ProjectId,
                    w.Project != null ? w.Project.Title : string.Empty,
                    w.Title, w.Status.ToString(), w.Priority.ToString(), 1.0))
                .ToListAsync(ct);
        }

        var scored = allEmbeddings
            .Select(e => (e.WorkItemId, Score: CosineSimilarity(queryEmbedding, e.Embedding)))
            .OrderByDescending(x => x.Score)
            .Take(request.TopN)
            .ToList();

        var topIds = scored.Select(s => s.WorkItemId).ToList();

        var workItems = await db.WorkItems
            .Where(w => topIds.Contains(w.Id))
            .Select(w => new
            {
                w.Id, w.ProjectId,
                ProjectTitle = w.Project != null ? w.Project.Title : string.Empty,
                w.Title, w.Status, w.Priority,
            })
            .ToListAsync(ct);

        return scored
            .Join(workItems, s => s.WorkItemId, w => w.Id,
                (s, w) => new AgentSearchResultDto(
                    w.Id, w.ProjectId, w.ProjectTitle,
                    w.Title, w.Status.ToString(), w.Priority.ToString(), Math.Round(s.Score, 4)))
            .ToList();
    }

    private static double CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length) return 0;
        double dot = 0, normA = 0, normB = 0;
        for (int i = 0; i < a.Length; i++)
        {
            dot  += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }
        return normA == 0 || normB == 0 ? 0 : dot / (Math.Sqrt(normA) * Math.Sqrt(normB));
    }
}

public sealed class AgentReindexHandler(IAppDbContext db, IEmbeddingService embedding)
    : IRequestHandler<AgentReindexCommand, AgentReindexResultDto>
{
    public async Task<AgentReindexResultDto> Handle(AgentReindexCommand _, CancellationToken ct)
    {
        var workItems = await db.WorkItems
            .Select(w => new { w.Id, w.Title, w.Description })
            .ToListAsync(ct);

        int indexed = 0, failed = 0;

        foreach (var wi in workItems)
        {
            try
            {
                var text = $"{wi.Title}. {wi.Description}".Trim('.');
                var vector = await embedding.GetEmbeddingAsync(text, ct);

                var existing = await db.WorkItemEmbeddings.FindAsync([wi.Id], ct);
                if (existing is null)
                    db.WorkItemEmbeddings.Add(WorkItemEmbedding.Create(wi.Id, vector, text));
                else
                    existing.Update(vector, text);

                await db.SaveChangesAsync(ct);
                indexed++;
            }
            catch
            {
                failed++;
            }
        }

        return new AgentReindexResultDto(indexed, failed);
    }
}

public sealed class AgentCreateTaskHandler(IAppDbContext db)
    : IRequestHandler<AgentCreateTaskCommand, int>
{
    public async Task<int> Handle(AgentCreateTaskCommand request, CancellationToken ct)
    {
        var project = await db.Projects.FindAsync([request.ProjectId], ct)
            ?? throw new KeyNotFoundException($"Proyecto {request.ProjectId} no encontrado.");

        if (!Enum.TryParse<WorkItemPriority>(request.Priority, out var priority))
            priority = WorkItemPriority.Medium;

        var sortOrder = await db.WorkItems.CountAsync(w => w.ProjectId == request.ProjectId, ct);

        var wi = WorkItem.Create(
            request.ProjectId, request.Title, request.Description,
            priority, request.EpicId, sortOrder,
            null, false, null, null, request.SprintId);

        if (request.AssignToSelf)
        {
            var person = await db.Persons.FindAsync([request.PersonId], ct);
            if (person is not null)
                wi.AddAssignee(person);
        }

        db.WorkItems.Add(wi);
        await db.SaveChangesAsync(ct);
        return wi.Id;
    }
}

public sealed class AgentUpdateTaskStatusHandler(IWorkItemLifecycleService service)
    : IRequestHandler<AgentUpdateTaskStatusCommand>
{
    public async Task Handle(AgentUpdateTaskStatusCommand request, CancellationToken ct)
    {
        if (!Enum.TryParse<WorkItemStatus>(request.NewStatus, out var status))
            throw new InvalidOperationException(
                $"Estado '{request.NewStatus}' no válido. Valores: Backlog, ToDo, InProgress, Blocked, Done, Discarded.");

        await service.TransitionStatusAsync(request.WorkItemId, status, request.PersonId, ct);
    }
}

public sealed class AgentAddCommentHandler(IAppDbContext db)
    : IRequestHandler<AgentAddCommentCommand>
{
    public async Task Handle(AgentAddCommentCommand request, CancellationToken ct)
    {
        var wi = await db.WorkItems.FindAsync([request.WorkItemId], ct)
            ?? throw new KeyNotFoundException($"Tarea {request.WorkItemId} no encontrada.");

        var comment = Comment.Create(request.WorkItemId, request.PersonId, request.Text);
        db.Comments.Add(comment);
        await db.SaveChangesAsync(ct);
    }
}

public sealed class AgentAddProjectNoteHandler(IAppDbContext db)
    : IRequestHandler<AgentAddProjectNoteCommand, int>
{
    public async Task<int> Handle(AgentAddProjectNoteCommand request, CancellationToken ct)
    {
        var project = await db.Projects.FindAsync([request.ProjectId], ct)
            ?? throw new KeyNotFoundException($"Proyecto {request.ProjectId} no encontrado.");

        var author = await db.Persons.FindAsync([request.PersonId], ct)
            ?? throw new KeyNotFoundException("Persona no encontrada.");

        var note = ProjectNote.Create(request.ProjectId, request.PersonId, request.Text);
        db.ProjectNotes.Add(note);
        await db.SaveChangesAsync(ct);
        return note.Id;
    }
}
