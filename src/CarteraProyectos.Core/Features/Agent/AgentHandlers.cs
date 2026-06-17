using CarteraProyectos.Core.Common;
using CarteraProyectos.Core.Domain;
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
public record AgentGetProjectsQuery(int PersonId, string? SiptGroup = null, string? Status = null) : IRequest<IReadOnlyList<AgentProjectSummaryDto>>;
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

public sealed class AgentGetMyTasksHandler(IAppDbContext db)
    : IRequestHandler<AgentGetMyTasksQuery, AgentMyTasksDto>
{
    public async Task<AgentMyTasksDto> Handle(AgentGetMyTasksQuery request, CancellationToken ct)
    {
        var person = await db.Persons.FindAsync([request.PersonId], ct)
            ?? throw new KeyNotFoundException("Persona no encontrada.");

        var myWorkItems = await db.WorkItems
            .Where(w => w.Assignees.Any(a => a.Id == request.PersonId))
            .Select(w => new
            {
                w.Id, w.Title, w.Status, w.Priority,
                ProjectTitle = w.Project != null ? w.Project.Title : string.Empty,
                FirstAssignee = w.Assignees.FirstOrDefault() != null ? w.Assignees.First().Name : null,
                w.EstimationHours, w.EstimationPoints, w.DueDate,
            })
            .ToListAsync(ct);

        var active = myWorkItems
            .Where(w => w.Status == WorkItemStatus.InProgress || w.Status == WorkItemStatus.Blocked || w.Status == WorkItemStatus.ToDo)
            .OrderBy(w => w.Priority == WorkItemPriority.Critical ? 0 : w.Priority == WorkItemPriority.High ? 1 : 2)
            .Select(w => new AgentTaskSummaryDto(w.Id, w.Title, w.Status.ToString(), w.Priority.ToString(),
                w.FirstAssignee, w.EstimationHours, w.EstimationPoints, w.DueDate?.ToString()))
            .ToList();

        var backlog = myWorkItems
            .Where(w => w.Status == WorkItemStatus.Backlog)
            .Select(w => new AgentTaskSummaryDto(w.Id, w.Title, w.Status.ToString(), w.Priority.ToString(),
                w.FirstAssignee, w.EstimationHours, w.EstimationPoints, w.DueDate?.ToString()))
            .ToList();

        return new AgentMyTasksDto(person.Name, person.Role.ToString(), active, backlog,
            myWorkItems.Count(w => w.Status == WorkItemStatus.Done));
    }
}

public sealed class AgentGetProjectsHandler(IAppDbContext db)
    : IRequestHandler<AgentGetProjectsQuery, IReadOnlyList<AgentProjectSummaryDto>>
{
    public async Task<IReadOnlyList<AgentProjectSummaryDto>> Handle(AgentGetProjectsQuery request, CancellationToken ct)
    {
        var person = await db.Persons.FindAsync([request.PersonId], ct)
            ?? throw new KeyNotFoundException("Persona no encontrada.");

        IQueryable<Project> baseQuery;

        if (person.Role == PersonRole.Gestor)
        {
            // Gestor ve toda la cartera
            baseQuery = db.Projects;
        }
        else
        {
            // Resto de roles: solo proyectos de sus equipos
            var myTeamIds = await db.PersonTeamMemberships
                .Where(m => m.PersonId == request.PersonId)
                .Select(m => m.TeamId)
                .ToListAsync(ct);

            var myProjectIds = await db.ProjectTeamAssignments
                .Where(a => myTeamIds.Contains(a.TeamId))
                .Select(a => a.ProjectId)
                .Distinct()
                .ToListAsync(ct);

            baseQuery = db.Projects.Where(p => myProjectIds.Contains(p.Id));
        }

        if (!string.IsNullOrWhiteSpace(request.SiptGroup) &&
            Enum.TryParse<SiptGroup>(request.SiptGroup, out var siptGroup))
            baseQuery = baseQuery.Where(p => p.SiptGroup == siptGroup);

        if (!string.IsNullOrWhiteSpace(request.Status) &&
            Enum.TryParse<ProjectStatus>(request.Status, out var status))
            baseQuery = baseQuery.Where(p => p.Status == status);

        var projects = await baseQuery
            .Select(p => new
            {
                p.Id, p.Title, p.Status, p.RequestingUnit,
                PrimaryTeamName = db.ProjectTeamAssignments
                    .Where(a => a.ProjectId == p.Id && a.IsPrimary)
                    .Select(a => a.Team.Name)
                    .FirstOrDefault(),
                TotalTasks    = db.WorkItems.Count(w => w.ProjectId == p.Id),
                DoneTasks     = db.WorkItems.Count(w => w.ProjectId == p.Id && w.Status == WorkItemStatus.Done),
                ActiveSprints = db.Sprints.Count(s => s.ProjectId == p.Id && s.Status == SprintStatus.Active),
            })
            .OrderByDescending(p => p.Id)
            .ToListAsync(ct);

        return projects.Select(p => new AgentProjectSummaryDto(
            p.Id, p.Title, p.Status.ToString(), p.RequestingUnit, p.PrimaryTeamName,
            p.TotalTasks, p.DoneTasks, p.ActiveSprints)).ToList();
    }
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
            .Where(w => w.ProjectId == request.ProjectId && w.Status != WorkItemStatus.Done)
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

public sealed class AgentGetCapacityHandler(IAppDbContext db)
    : IRequestHandler<AgentGetCapacityQuery, AgentCapacityDto>
{
    public async Task<AgentCapacityDto> Handle(AgentGetCapacityQuery _, CancellationToken ct)
    {
        var teams = await db.Teams
            .Include(t => t.Members).ThenInclude(m => m.Person)
            .Include(t => t.Projects).ThenInclude(pta => pta.Project)
            .OrderBy(t => t.Name)
            .ToListAsync(ct);

        var activeStatuses = new[]
        {
            ProjectStatus.PlanningWithClient, ProjectStatus.PlanningSprint,
            ProjectStatus.InSprint, ProjectStatus.DevelopmentOutsideSprint, ProjectStatus.InTesting,
        };
        var result = new List<AgentTeamCapacityDto>();

        foreach (var team in teams)
        {
            var activeProjectCount = team.Projects.Count(pta => pta.Project != null && activeStatuses.Contains(pta.Project.Status));
            var members = new List<AgentMemberCapacityDto>();

            foreach (var m in team.Members)
            {
                if (m.Person is null) continue;
                var activeTasks = await db.WorkItems.CountAsync(
                    w => w.Assignees.Any(a => a.Id == m.Person.Id) &&
                         (w.Status == WorkItemStatus.InProgress || w.Status == WorkItemStatus.Blocked), ct);
                var pendingTasks = await db.WorkItems.CountAsync(
                    w => w.Assignees.Any(a => a.Id == m.Person.Id) &&
                         (w.Status == WorkItemStatus.ToDo || w.Status == WorkItemStatus.Backlog), ct);
                var load = activeTasks <= 3 ? "Green" : activeTasks <= 6 ? "Yellow" : "Red";
                members.Add(new AgentMemberCapacityDto(m.Person.Name, m.Person.Role.ToString(), activeTasks, pendingTasks, load));
            }

            result.Add(new AgentTeamCapacityDto(team.Id, team.Name, activeProjectCount,
                members.OrderByDescending(m => m.ActiveTasks).ToList()));
        }

        return new AgentCapacityDto(result);
    }
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

public sealed class AgentUpdateTaskStatusHandler(IAppDbContext db)
    : IRequestHandler<AgentUpdateTaskStatusCommand>
{
    public async Task Handle(AgentUpdateTaskStatusCommand request, CancellationToken ct)
    {
        var wi = await db.WorkItems.FindAsync([request.WorkItemId], ct)
            ?? throw new KeyNotFoundException($"Tarea {request.WorkItemId} no encontrada.");

        if (!Enum.TryParse<WorkItemStatus>(request.NewStatus, out var status))
            throw new InvalidOperationException($"Estado '{request.NewStatus}' no válido.");

        wi.TransitionStatus(status);
        await db.SaveChangesAsync(ct);
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
