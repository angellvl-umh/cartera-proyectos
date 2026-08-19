using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Features.Sprints;
using CarteraProyectos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace CarteraProyectos.UnitTests.Features.Sprints;

/// <summary>
/// Tests para la lógica de carry-over y snapshot de puntos al completar sprints.
/// </summary>
public class SprintCarryOverTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static async Task<(AppDbContext db, Project project)> DbWithProject()
    {
        var db = CreateDb();
        var project = Project.Create("Proyecto Test", null, "TIC", ProjectComplexity.VerySmall, null, null, null);
        db.Projects.Add(project);
        await db.SaveChangesAsync();
        return (db, project);
    }

    /// <summary>
    /// Helper: crea un sprint en estado Planning y lo guarda en DB.
    /// </summary>
    private static async Task<Sprint> AddSprintAsync(AppDbContext db, int projectId,
        SprintStatus targetStatus = SprintStatus.Planning, string name = "Sprint 1")
    {
        var sprint = Sprint.Create(projectId, name, null, null, null, null);
        if (targetStatus >= SprintStatus.Active) sprint.TransitionStatus(SprintStatus.Active);
        if (targetStatus == SprintStatus.Completed) sprint.TransitionStatus(SprintStatus.Completed);
        db.Sprints.Add(sprint);
        await db.SaveChangesAsync();
        return sprint;
    }

    /// <summary>
    /// Helper: crea un WorkItem y lo guarda.
    /// </summary>
    private static async Task<WorkItem> AddWorkItemAsync(AppDbContext db, int projectId, int? sprintId,
        WorkItemStatus status = WorkItemStatus.ToDo, int? estimationPoints = null)
    {
        var wi = WorkItem.Create(projectId, "Tarea", null, WorkItemPriority.Medium, null, 0,
            null, false, null, null, sprintId, estimationPoints);
        // Transicionar al estado deseado
        if (status != WorkItemStatus.Backlog)
            wi.TransitionStatus(status);
        db.WorkItems.Add(wi);
        await db.SaveChangesAsync();
        return wi;
    }

    private static TransitionSprintStatusHandler CreateHandler(AppDbContext db)
        => new(new SprintLifecycleService(db));

    // ── Test 1: CommittedPoints al activar ─────────────────────────────────

    [Fact]
    public async Task ActivateSprint_SnapshotsCommittedPoints_SumOfEstimationPoints()
    {
        var (db, project) = await DbWithProject();
        var sprint = await AddSprintAsync(db, project.Id);

        // Tareas: 3 pts, 5 pts y una sin puntos
        await AddWorkItemAsync(db, project.Id, sprint.Id, WorkItemStatus.ToDo, 3);
        await AddWorkItemAsync(db, project.Id, sprint.Id, WorkItemStatus.ToDo, 5);
        await AddWorkItemAsync(db, project.Id, sprint.Id, WorkItemStatus.ToDo, null);

        var handler = CreateHandler(db);
        await handler.Handle(new TransitionSprintStatusCommand(sprint.Id, SprintStatus.Active), CancellationToken.None);

        var updated = await db.Sprints.FindAsync(sprint.Id);
        updated!.CommittedPoints.ShouldBe(8);
    }

    // ── Test 2: DeliveredPoints al completar ───────────────────────────────

    [Fact]
    public async Task CompleteSprint_AllDoneOrDiscarded_SnapshotsDeliveredPoints_NoCarryOverNeeded()
    {
        var (db, project) = await DbWithProject();
        var sprint = await AddSprintAsync(db, project.Id, SprintStatus.Active);

        // Done: 3 pts, Discarded: 5 pts (no se suman los Discarded)
        var doneItem = await AddWorkItemAsync(db, project.Id, sprint.Id, WorkItemStatus.Done, 3);
        var discardedItem = await AddWorkItemAsync(db, project.Id, sprint.Id, WorkItemStatus.Discarded, 5);

        var handler = CreateHandler(db);
        await handler.Handle(new TransitionSprintStatusCommand(sprint.Id, SprintStatus.Completed), CancellationToken.None);

        var updated = await db.Sprints.FindAsync(sprint.Id);
        updated!.Status.ShouldBe(SprintStatus.Completed);
        updated.DeliveredPoints.ShouldBe(3);
    }

    // ── Test 3: Sin CarryOver con tareas sin terminar → excepción ──────────

    [Fact]
    public async Task CompleteSprint_WithInProgressTask_NoCarryOver_ThrowsInvalidOperationException()
    {
        var (db, project) = await DbWithProject();
        var sprint = await AddSprintAsync(db, project.Id, SprintStatus.Active);

        await AddWorkItemAsync(db, project.Id, sprint.Id, WorkItemStatus.InProgress);

        var handler = CreateHandler(db);
        var ex = await Should.ThrowAsync<InvalidOperationException>(
            () => handler.Handle(new TransitionSprintStatusCommand(sprint.Id, SprintStatus.Completed), CancellationToken.None));

        ex.Message.ShouldContain("carry-over");
        // Sprint no cambia de estado
        var unchanged = await db.Sprints.FindAsync(sprint.Id);
        unchanged!.Status.ShouldBe(SprintStatus.Active);
    }

    // ── Test 4: CarryOver = Backlog ─────────────────────────────────────────

    [Fact]
    public async Task CompleteSprint_CarryOverBacklog_MovesTaskToBacklogWithHistory()
    {
        var (db, project) = await DbWithProject();
        var sprint = await AddSprintAsync(db, project.Id, SprintStatus.Active);

        var wi = await AddWorkItemAsync(db, project.Id, sprint.Id, WorkItemStatus.InProgress);

        var handler = CreateHandler(db);
        await handler.Handle(new TransitionSprintStatusCommand(
            sprint.Id, SprintStatus.Completed,
            CarryOver: CarryOverTarget.Backlog), CancellationToken.None);

        var updatedSprint = await db.Sprints.FindAsync(sprint.Id);
        updatedSprint!.Status.ShouldBe(SprintStatus.Completed);

        var updatedWi = await db.WorkItems.FindAsync(wi.Id);
        updatedWi!.SprintId.ShouldBeNull();
        updatedWi.Status.ShouldBe(WorkItemStatus.Backlog);

        // Debe existir entrada en WorkItemStatusHistories
        var history = await db.WorkItemStatusHistories
            .Where(h => h.WorkItemId == wi.Id)
            .ToListAsync();
        history.Count.ShouldBe(1);
        history[0].FromStatus.ShouldBe(WorkItemStatus.InProgress);
        history[0].ToStatus.ShouldBe(WorkItemStatus.Backlog);
    }

    [Fact]
    public async Task CompleteSprint_CarryOverBacklog_TaskAlreadyBacklog_NoHistoryEntry()
    {
        var (db, project) = await DbWithProject();
        var sprint = await AddSprintAsync(db, project.Id, SprintStatus.Active);

        // Tarea ya en Backlog pero asignada al sprint
        var wi = await AddWorkItemAsync(db, project.Id, sprint.Id, WorkItemStatus.Backlog);

        var handler = CreateHandler(db);
        await handler.Handle(new TransitionSprintStatusCommand(
            sprint.Id, SprintStatus.Completed,
            CarryOver: CarryOverTarget.Backlog), CancellationToken.None);

        var updatedWi = await db.WorkItems.FindAsync(wi.Id);
        updatedWi!.SprintId.ShouldBeNull();
        updatedWi.Status.ShouldBe(WorkItemStatus.Backlog);

        // Sin entrada en el historial (no hubo cambio de estado)
        var history = await db.WorkItemStatusHistories
            .Where(h => h.WorkItemId == wi.Id)
            .ToListAsync();
        history.ShouldBeEmpty();
    }

    // ── Test 5: CarryOver = Sprint con destino válido ─────────────────────

    [Fact]
    public async Task CompleteSprint_CarryOverSprint_MovesTaskToTargetSprintKeepingStatus()
    {
        var (db, project) = await DbWithProject();
        var activeSprint = await AddSprintAsync(db, project.Id, SprintStatus.Active, "Sprint 1");
        var targetSprint = await AddSprintAsync(db, project.Id, SprintStatus.Planning, "Sprint 2");

        var wi = await AddWorkItemAsync(db, project.Id, activeSprint.Id, WorkItemStatus.InProgress);

        var handler = CreateHandler(db);
        await handler.Handle(new TransitionSprintStatusCommand(
            activeSprint.Id, SprintStatus.Completed,
            CarryOver: CarryOverTarget.Sprint,
            TargetSprintId: targetSprint.Id), CancellationToken.None);

        var updatedSprint = await db.Sprints.FindAsync(activeSprint.Id);
        updatedSprint!.Status.ShouldBe(SprintStatus.Completed);

        var updatedWi = await db.WorkItems.FindAsync(wi.Id);
        updatedWi!.SprintId.ShouldBe(targetSprint.Id);
        updatedWi.Status.ShouldBe(WorkItemStatus.InProgress); // conserva estado

        // No debe haber historia de cambio de estado para esta tarea
        var history = await db.WorkItemStatusHistories
            .Where(h => h.WorkItemId == wi.Id)
            .ToListAsync();
        history.ShouldBeEmpty();
    }

    // ── Test 6: Validaciones de CarryOver = Sprint ─────────────────────────

    [Fact]
    public async Task CompleteSprint_CarryOverSprint_WithoutTargetSprintId_Throws()
    {
        var (db, project) = await DbWithProject();
        var sprint = await AddSprintAsync(db, project.Id, SprintStatus.Active);
        await AddWorkItemAsync(db, project.Id, sprint.Id, WorkItemStatus.InProgress);

        var handler = CreateHandler(db);
        var ex = await Should.ThrowAsync<InvalidOperationException>(
            () => handler.Handle(new TransitionSprintStatusCommand(
                sprint.Id, SprintStatus.Completed,
                CarryOver: CarryOverTarget.Sprint), CancellationToken.None));

        ex.Message.ShouldContain("TargetSprintId");
    }

    [Fact]
    public async Task CompleteSprint_CarryOverSprint_SameSprintAsTarget_Throws()
    {
        var (db, project) = await DbWithProject();
        var sprint = await AddSprintAsync(db, project.Id, SprintStatus.Active);
        await AddWorkItemAsync(db, project.Id, sprint.Id, WorkItemStatus.InProgress);

        var handler = CreateHandler(db);
        var ex = await Should.ThrowAsync<InvalidOperationException>(
            () => handler.Handle(new TransitionSprintStatusCommand(
                sprint.Id, SprintStatus.Completed,
                CarryOver: CarryOverTarget.Sprint,
                TargetSprintId: sprint.Id), CancellationToken.None));

        ex.Message.ShouldContain("mismo sprint");
    }

    [Fact]
    public async Task CompleteSprint_CarryOverSprint_TargetFromOtherProject_Throws()
    {
        var (db, project) = await DbWithProject();

        // Segundo proyecto
        var otherProject = Project.Create("Otro Proyecto", null, "TIC", ProjectComplexity.Small, null, null, null);
        db.Projects.Add(otherProject);
        await db.SaveChangesAsync();

        var activeSprint = await AddSprintAsync(db, project.Id, SprintStatus.Active, "Sprint 1");
        var otherSprint = await AddSprintAsync(db, otherProject.Id, SprintStatus.Planning, "Sprint Otro");

        await AddWorkItemAsync(db, project.Id, activeSprint.Id, WorkItemStatus.InProgress);

        var handler = CreateHandler(db);
        var ex = await Should.ThrowAsync<InvalidOperationException>(
            () => handler.Handle(new TransitionSprintStatusCommand(
                activeSprint.Id, SprintStatus.Completed,
                CarryOver: CarryOverTarget.Sprint,
                TargetSprintId: otherSprint.Id), CancellationToken.None));

        ex.Message.ShouldContain("mismo proyecto");
    }

    [Fact]
    public async Task CompleteSprint_CarryOverSprint_TargetNotPlanning_Throws()
    {
        var (db, project) = await DbWithProject();
        var activeSprint = await AddSprintAsync(db, project.Id, SprintStatus.Active, "Sprint 1");
        var completedSprint = await AddSprintAsync(db, project.Id, SprintStatus.Completed, "Sprint Completado");

        await AddWorkItemAsync(db, project.Id, activeSprint.Id, WorkItemStatus.InProgress);

        var handler = CreateHandler(db);
        var ex = await Should.ThrowAsync<InvalidOperationException>(
            () => handler.Handle(new TransitionSprintStatusCommand(
                activeSprint.Id, SprintStatus.Completed,
                CarryOver: CarryOverTarget.Sprint,
                TargetSprintId: completedSprint.Id), CancellationToken.None));

        ex.Message.ShouldContain("Planning");
    }

    [Fact]
    public async Task CompleteSprint_CarryOverSprint_TargetNotFound_Throws()
    {
        var (db, project) = await DbWithProject();
        var activeSprint = await AddSprintAsync(db, project.Id, SprintStatus.Active);
        await AddWorkItemAsync(db, project.Id, activeSprint.Id, WorkItemStatus.InProgress);

        var handler = CreateHandler(db);
        var ex = await Should.ThrowAsync<InvalidOperationException>(
            () => handler.Handle(new TransitionSprintStatusCommand(
                activeSprint.Id, SprintStatus.Completed,
                CarryOver: CarryOverTarget.Sprint,
                TargetSprintId: 99999), CancellationToken.None));

        ex.Message.ShouldContain("no encontrado");
    }
}
