using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Features.Reports;
using CarteraProyectos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace CarteraProyectos.UnitTests.Features.Reports;

public class AgileMetricsHandlerTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static Project MakeProject(string title = "Proyecto Test")
        => Project.Create(title, null, "TIC", ProjectComplexity.Small, 2026, null, null);

    private static Person MakePerson(string name = "Dev")
        => Person.CreateFromClaims(Guid.NewGuid().ToString(), name, $"{name.ToLower()}@test.com");

    /// <summary>
    /// Crea un sprint ya en estado Completed con snapshots opcionales.
    /// </summary>
    private static Sprint MakeCompletedSprint(
        int projectId, string name,
        DateOnly? startDate = null, DateOnly? endDate = null,
        int? capacity = null,
        int? committedPoints = null, int? deliveredPoints = null)
    {
        var sprint = Sprint.Create(projectId, name, null, startDate, endDate, capacity);
        sprint.TransitionStatus(SprintStatus.Active);
        sprint.TransitionStatus(SprintStatus.Completed);
        if (committedPoints.HasValue) sprint.SnapshotCommitted(committedPoints.Value);
        if (deliveredPoints.HasValue) sprint.SnapshotDelivered(deliveredPoints.Value);
        return sprint;
    }

    /// <summary>
    /// Crea un WorkItem con EstimationPoints y status dado.
    /// </summary>
    private static WorkItem MakeWorkItem(int projectId, int? sprintId = null,
        WorkItemStatus status = WorkItemStatus.Backlog, int? estimationPoints = null)
    {
        var wi = WorkItem.Create(projectId, "Tarea", null, WorkItemPriority.Medium, null, 0,
            null, false, null, null, sprintId, estimationPoints);
        if (status != WorkItemStatus.Backlog) wi.TransitionStatus(status);
        return wi;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // FEATURE 1 — GetProjectVelocity
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Velocity_TwoCompletedSprintsWithSnapshot_CorrectAverage()
    {
        await using var db = CreateDb();
        var project = MakeProject();
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        var s1 = MakeCompletedSprint(project.Id, "Sprint 1",
            new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 14),
            committedPoints: 30, deliveredPoints: 25);
        var s2 = MakeCompletedSprint(project.Id, "Sprint 2",
            new DateOnly(2026, 1, 15), new DateOnly(2026, 1, 28),
            committedPoints: 40, deliveredPoints: 35);
        db.Sprints.AddRange(s1, s2);
        await db.SaveChangesAsync();

        var handler = new GetProjectVelocityHandler(db);
        var result = await handler.Handle(new GetProjectVelocityQuery(project.Id), CancellationToken.None);

        result.ProjectId.ShouldBe(project.Id);
        result.Sprints.Count.ShouldBe(2);
        result.Sprints[0].Name.ShouldBe("Sprint 1");
        result.Sprints[0].CommittedPoints.ShouldBe(30);
        result.Sprints[0].DeliveredPoints.ShouldBe(25);
        result.Sprints[1].DeliveredPoints.ShouldBe(35);
        result.AverageVelocity.ShouldBe(30.0); // (25 + 35) / 2
    }

    [Fact]
    public async Task Velocity_SprintWithoutSnapshot_CalculatesOnTheFly()
    {
        await using var db = CreateDb();
        var project = MakeProject();
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        // Sprint sin snapshots
        var sprint = MakeCompletedSprint(project.Id, "Sprint Antiguo",
            new DateOnly(2025, 6, 1), new DateOnly(2025, 6, 14));
        db.Sprints.Add(sprint);
        await db.SaveChangesAsync();

        // 2 tareas con puntos: una Done (suma al delivered), otra InProgress
        var wi1 = MakeWorkItem(project.Id, sprint.Id, WorkItemStatus.Done, estimationPoints: 8);
        var wi2 = MakeWorkItem(project.Id, sprint.Id, WorkItemStatus.InProgress, estimationPoints: 5);
        db.WorkItems.AddRange(wi1, wi2);
        await db.SaveChangesAsync();

        var handler = new GetProjectVelocityHandler(db);
        var result = await handler.Handle(new GetProjectVelocityQuery(project.Id), CancellationToken.None);

        result.Sprints.Count.ShouldBe(1);
        result.Sprints[0].CommittedPoints.ShouldBe(13); // 8 + 5
        result.Sprints[0].DeliveredPoints.ShouldBe(8);  // solo Done
        result.AverageVelocity.ShouldBe(8.0);
    }

    [Fact]
    public async Task Velocity_ProjectNotFound_ThrowsKeyNotFoundException()
    {
        await using var db = CreateDb();
        var handler = new GetProjectVelocityHandler(db);

        await Should.ThrowAsync<KeyNotFoundException>(
            () => handler.Handle(new GetProjectVelocityQuery(999), CancellationToken.None));
    }

    [Fact]
    public async Task Velocity_NoCompletedSprints_ReturnsEmptyListAndNullAverage()
    {
        await using var db = CreateDb();
        var project = MakeProject();
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        // Sprint en Planning (no Completed)
        var sprint = Sprint.Create(project.Id, "Sprint 1", null, null, null, null);
        db.Sprints.Add(sprint);
        await db.SaveChangesAsync();

        var handler = new GetProjectVelocityHandler(db);
        var result = await handler.Handle(new GetProjectVelocityQuery(project.Id), CancellationToken.None);

        result.Sprints.ShouldBeEmpty();
        result.AverageVelocity.ShouldBeNull();
    }

    [Fact]
    public async Task Velocity_SprintsOrderedChronologically()
    {
        await using var db = CreateDb();
        var project = MakeProject();
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        // Creados en orden inverso de fecha
        var s2 = MakeCompletedSprint(project.Id, "Sprint B",
            new DateOnly(2026, 3, 1), committedPoints: 20, deliveredPoints: 18);
        var s1 = MakeCompletedSprint(project.Id, "Sprint A",
            new DateOnly(2026, 1, 1), committedPoints: 10, deliveredPoints: 10);
        db.Sprints.AddRange(s2, s1);
        await db.SaveChangesAsync();

        var handler = new GetProjectVelocityHandler(db);
        var result = await handler.Handle(new GetProjectVelocityQuery(project.Id), CancellationToken.None);

        result.Sprints[0].Name.ShouldBe("Sprint A");
        result.Sprints[1].Name.ShouldBe("Sprint B");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // FEATURE 2 — GetSprintBurndown
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Burndown_FiveDaySprintWithTasks_CorrectRemainingPointsPerDay()
    {
        await using var db = CreateDb();
        var project = MakeProject();
        var person = MakePerson();
        db.Projects.Add(project);
        db.Persons.Add(person);
        await db.SaveChangesAsync();

        var startDate = new DateOnly(2026, 6, 1);
        var endDate   = new DateOnly(2026, 6, 5);

        var sprint = Sprint.Create(project.Id, "Sprint BDN", null, startDate, endDate, null);
        sprint.TransitionStatus(SprintStatus.Active);
        sprint.SnapshotCommitted(10);
        db.Sprints.Add(sprint);
        await db.SaveChangesAsync();

        // 2 tareas: 4 pts y 6 pts
        var wi1 = MakeWorkItem(project.Id, sprint.Id, WorkItemStatus.Done, estimationPoints: 4);
        var wi2 = MakeWorkItem(project.Id, sprint.Id, WorkItemStatus.Done, estimationPoints: 6);
        db.WorkItems.AddRange(wi1, wi2);
        await db.SaveChangesAsync();

        // wi1 completada el día 2 (2026-06-02), wi2 el día 4 (2026-06-04)
        var day2 = new DateTime(2026, 6, 2, 12, 0, 0, DateTimeKind.Utc);
        var day4 = new DateTime(2026, 6, 4, 12, 0, 0, DateTimeKind.Utc);

        db.WorkItemStatusHistories.Add(WorkItemStatusHistory.Create(wi1, WorkItemStatus.InProgress, WorkItemStatus.Done, person.Id, day2));
        db.WorkItemStatusHistories.Add(WorkItemStatusHistory.Create(wi2, WorkItemStatus.InProgress, WorkItemStatus.Done, person.Id, day4));
        await db.SaveChangesAsync();

        var handler = new GetSprintBurndownHandler(db);
        var result = await handler.Handle(new GetSprintBurndownQuery(project.Id, sprint.Id), CancellationToken.None);

        result.SprintId.ShouldBe(sprint.Id);
        result.TotalPoints.ShouldBe(10);
        result.Days.Count.ShouldBe(5); // 1..5 inclusive

        // Día 1 (2026-06-01): ninguna completada → remaining = 10
        var day1Dto = result.Days.First(d => d.Date == "2026-06-01");
        day1Dto.RemainingPoints.ShouldBe(10);

        // Día 2 (2026-06-02): wi1 completada (4 pts) → remaining = 6
        var day2Dto = result.Days.First(d => d.Date == "2026-06-02");
        day2Dto.RemainingPoints.ShouldBe(6);

        // Día 3 (2026-06-03): mismas que día 2 → remaining = 6
        var day3Dto = result.Days.First(d => d.Date == "2026-06-03");
        day3Dto.RemainingPoints.ShouldBe(6);

        // Día 4 (2026-06-04): wi2 completada (6 pts) → remaining = 0
        var day4Dto = result.Days.First(d => d.Date == "2026-06-04");
        day4Dto.RemainingPoints.ShouldBe(0);

        // Día 5 (2026-06-05): remaining = 0
        var day5Dto = result.Days.First(d => d.Date == "2026-06-05");
        day5Dto.RemainingPoints.ShouldBe(0);
    }

    [Fact]
    public async Task Burndown_IdealLine_StartsAtTotalAndEndsAtZero()
    {
        await using var db = CreateDb();
        var project = MakeProject();
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        var startDate = new DateOnly(2026, 6, 1);
        var endDate   = new DateOnly(2026, 6, 5);

        var sprint = Sprint.Create(project.Id, "Sprint Ideal", null, startDate, endDate, null);
        sprint.TransitionStatus(SprintStatus.Active);
        sprint.SnapshotCommitted(20);
        db.Sprints.Add(sprint);
        await db.SaveChangesAsync();

        var handler = new GetSprintBurndownHandler(db);
        var result = await handler.Handle(new GetSprintBurndownQuery(project.Id, sprint.Id), CancellationToken.None);

        result.Days[0].IdealPoints.ShouldBe(20.0); // día 0 → totalPoints
        result.Days[^1].IdealPoints.ShouldBe(0.0); // último día → 0
    }

    [Fact]
    public async Task Burndown_SprintWithoutDates_ThrowsInvalidOperationException()
    {
        await using var db = CreateDb();
        var project = MakeProject();
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        var sprint = Sprint.Create(project.Id, "Sin Fechas", null, null, null, null);
        db.Sprints.Add(sprint);
        await db.SaveChangesAsync();

        var handler = new GetSprintBurndownHandler(db);

        await Should.ThrowAsync<InvalidOperationException>(
            () => handler.Handle(new GetSprintBurndownQuery(project.Id, sprint.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Burndown_SprintNotFound_ThrowsKeyNotFoundException()
    {
        await using var db = CreateDb();
        var project = MakeProject();
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        var handler = new GetSprintBurndownHandler(db);

        await Should.ThrowAsync<KeyNotFoundException>(
            () => handler.Handle(new GetSprintBurndownQuery(project.Id, 9999), CancellationToken.None));
    }

    [Fact]
    public async Task Burndown_FutureDays_HaveNullRemainingPoints()
    {
        await using var db = CreateDb();
        var project = MakeProject();
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        // Sprint con fechas en el futuro (bien pasadas de hoy para garantizar que son futuras)
        var futureStart = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10));
        var futureEnd   = futureStart.AddDays(4);

        var sprint = Sprint.Create(project.Id, "Sprint Futuro", null, futureStart, futureEnd, null);
        sprint.SnapshotCommitted(10);
        db.Sprints.Add(sprint);
        await db.SaveChangesAsync();

        var handler = new GetSprintBurndownHandler(db);
        var result = await handler.Handle(new GetSprintBurndownQuery(project.Id, sprint.Id), CancellationToken.None);

        // Todos los días son futuros → RemainingPoints null
        result.Days.ShouldAllBe(d => d.RemainingPoints == null);
    }

    [Fact]
    public async Task Burndown_DiscardedTask_CountsAsCompleted()
    {
        await using var db = CreateDb();
        var project = MakeProject();
        var person = MakePerson();
        db.Projects.Add(project);
        db.Persons.Add(person);
        await db.SaveChangesAsync();

        var startDate = new DateOnly(2026, 6, 1);
        var endDate   = new DateOnly(2026, 6, 3);

        var sprint = Sprint.Create(project.Id, "Sprint Discard", null, startDate, endDate, null);
        sprint.TransitionStatus(SprintStatus.Active);
        sprint.SnapshotCommitted(10);
        db.Sprints.Add(sprint);
        await db.SaveChangesAsync();

        var wi = MakeWorkItem(project.Id, sprint.Id, WorkItemStatus.Discarded, estimationPoints: 10);
        db.WorkItems.Add(wi);
        await db.SaveChangesAsync();

        // Discarded el día 2
        var day2 = new DateTime(2026, 6, 2, 8, 0, 0, DateTimeKind.Utc);
        db.WorkItemStatusHistories.Add(WorkItemStatusHistory.Create(wi, WorkItemStatus.InProgress, WorkItemStatus.Discarded, person.Id, day2));
        await db.SaveChangesAsync();

        var handler = new GetSprintBurndownHandler(db);
        var result = await handler.Handle(new GetSprintBurndownQuery(project.Id, sprint.Id), CancellationToken.None);

        // Día 1: aún no descartada → 10 restantes
        result.Days.First(d => d.Date == "2026-06-01").RemainingPoints.ShouldBe(10);
        // Día 2: descartada → 0 restantes
        result.Days.First(d => d.Date == "2026-06-02").RemainingPoints.ShouldBe(0);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // FEATURE 3 — GetProjectCycleTime
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CycleTime_FullHistoryBacklogToDone_CorrectCycleAndLeadTime()
    {
        await using var db = CreateDb();
        var project = MakeProject();
        var person = MakePerson();
        db.Projects.Add(project);
        db.Persons.Add(person);
        await db.SaveChangesAsync();

        var wi = MakeWorkItem(project.Id, status: WorkItemStatus.Done);
        db.WorkItems.Add(wi);
        await db.SaveChangesAsync();

        // Histórico: Backlog(creación) → ToDo → InProgress → Done
        var t0 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);  // creación
        var t1 = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc);  // → ToDo
        var t2 = new DateTime(2026, 1, 4, 0, 0, 0, DateTimeKind.Utc);  // → InProgress
        var t3 = new DateTime(2026, 1, 9, 0, 0, 0, DateTimeKind.Utc);  // → Done

        db.WorkItemStatusHistories.Add(WorkItemStatusHistory.Create(wi, null, WorkItemStatus.Backlog, person.Id, t0));
        db.WorkItemStatusHistories.Add(WorkItemStatusHistory.Create(wi, WorkItemStatus.Backlog, WorkItemStatus.ToDo, person.Id, t1));
        db.WorkItemStatusHistories.Add(WorkItemStatusHistory.Create(wi, WorkItemStatus.ToDo, WorkItemStatus.InProgress, person.Id, t2));
        db.WorkItemStatusHistories.Add(WorkItemStatusHistory.Create(wi, WorkItemStatus.InProgress, WorkItemStatus.Done, person.Id, t3));
        await db.SaveChangesAsync();

        var handler = new GetProjectCycleTimeHandler(db);
        var result = await handler.Handle(new GetProjectCycleTimeQuery(project.Id), CancellationToken.None);

        result.CompletedItemsCount.ShouldBe(1);
        result.Items.Count.ShouldBe(1);

        // LeadTime: t3 - t0 = 8 días
        result.Items[0].LeadTimeDays.ShouldBe(8.0);

        // CycleTime: t3 - t2 = 5 días
        result.Items[0].CycleTimeDays.ShouldBe(5.0);

        result.AverageCycleTimeDays.ShouldBe(5.0);
        result.AverageLeadTimeDays.ShouldBe(8.0);
    }

    [Fact]
    public async Task CycleTime_DoneWithoutInProgress_CycleTimeIsNull()
    {
        await using var db = CreateDb();
        var project = MakeProject();
        var person = MakePerson();
        db.Projects.Add(project);
        db.Persons.Add(person);
        await db.SaveChangesAsync();

        var wi = MakeWorkItem(project.Id, status: WorkItemStatus.Done);
        db.WorkItems.Add(wi);
        await db.SaveChangesAsync();

        // Sin paso por InProgress: Backlog → ToDo → Done
        var t0 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var t1 = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc);
        var t2 = new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc);

        db.WorkItemStatusHistories.Add(WorkItemStatusHistory.Create(wi, null, WorkItemStatus.Backlog, person.Id, t0));
        db.WorkItemStatusHistories.Add(WorkItemStatusHistory.Create(wi, WorkItemStatus.Backlog, WorkItemStatus.ToDo, person.Id, t1));
        db.WorkItemStatusHistories.Add(WorkItemStatusHistory.Create(wi, WorkItemStatus.ToDo, WorkItemStatus.Done, person.Id, t2));
        await db.SaveChangesAsync();

        var handler = new GetProjectCycleTimeHandler(db);
        var result = await handler.Handle(new GetProjectCycleTimeQuery(project.Id), CancellationToken.None);

        result.Items[0].CycleTimeDays.ShouldBeNull();
        result.Items[0].LeadTimeDays.ShouldBe(4.0); // t2 - t0 = 4 días
        result.AverageCycleTimeDays.ShouldBeNull(); // no hay cycle times válidos
    }

    [Fact]
    public async Task CycleTime_TwoItems_AveragesCalculatedCorrectly()
    {
        await using var db = CreateDb();
        var project = MakeProject();
        var person = MakePerson();
        db.Projects.Add(project);
        db.Persons.Add(person);
        await db.SaveChangesAsync();

        var wi1 = MakeWorkItem(project.Id, status: WorkItemStatus.Done);
        var wi2 = MakeWorkItem(project.Id, status: WorkItemStatus.Done);
        db.WorkItems.AddRange(wi1, wi2);
        await db.SaveChangesAsync();

        // wi1: cycle = 2 días, lead = 4 días
        var w1t0 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var w1t1 = new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc); // InProgress
        var w1t2 = new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc); // Done

        db.WorkItemStatusHistories.Add(WorkItemStatusHistory.Create(wi1, null, WorkItemStatus.Backlog, person.Id, w1t0));
        db.WorkItemStatusHistories.Add(WorkItemStatusHistory.Create(wi1, WorkItemStatus.Backlog, WorkItemStatus.InProgress, person.Id, w1t1));
        db.WorkItemStatusHistories.Add(WorkItemStatusHistory.Create(wi1, WorkItemStatus.InProgress, WorkItemStatus.Done, person.Id, w1t2));

        // wi2: cycle = 4 días, lead = 6 días
        var w2t0 = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        var w2t1 = new DateTime(2026, 2, 3, 0, 0, 0, DateTimeKind.Utc); // InProgress
        var w2t2 = new DateTime(2026, 2, 7, 0, 0, 0, DateTimeKind.Utc); // Done

        db.WorkItemStatusHistories.Add(WorkItemStatusHistory.Create(wi2, null, WorkItemStatus.Backlog, person.Id, w2t0));
        db.WorkItemStatusHistories.Add(WorkItemStatusHistory.Create(wi2, WorkItemStatus.Backlog, WorkItemStatus.InProgress, person.Id, w2t1));
        db.WorkItemStatusHistories.Add(WorkItemStatusHistory.Create(wi2, WorkItemStatus.InProgress, WorkItemStatus.Done, person.Id, w2t2));

        await db.SaveChangesAsync();

        var handler = new GetProjectCycleTimeHandler(db);
        var result = await handler.Handle(new GetProjectCycleTimeQuery(project.Id), CancellationToken.None);

        result.CompletedItemsCount.ShouldBe(2);
        result.AverageCycleTimeDays.ShouldBe(3.0);  // (2 + 4) / 2
        result.AverageLeadTimeDays.ShouldBe(5.0);   // (4 + 6) / 2
    }

    [Fact]
    public async Task CycleTime_ProjectNotFound_ThrowsKeyNotFoundException()
    {
        await using var db = CreateDb();
        var handler = new GetProjectCycleTimeHandler(db);

        await Should.ThrowAsync<KeyNotFoundException>(
            () => handler.Handle(new GetProjectCycleTimeQuery(999), CancellationToken.None));
    }

    [Fact]
    public async Task CycleTime_NoDoneItems_ReturnsNullAveragesAndEmptyList()
    {
        await using var db = CreateDb();
        var project = MakeProject();
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        var wi = MakeWorkItem(project.Id, status: WorkItemStatus.InProgress);
        db.WorkItems.Add(wi);
        await db.SaveChangesAsync();

        var handler = new GetProjectCycleTimeHandler(db);
        var result = await handler.Handle(new GetProjectCycleTimeQuery(project.Id), CancellationToken.None);

        result.CompletedItemsCount.ShouldBe(0);
        result.Items.ShouldBeEmpty();
        result.AverageCycleTimeDays.ShouldBeNull();
        result.AverageLeadTimeDays.ShouldBeNull();
    }

    [Fact]
    public async Task CycleTime_ItemsOrderedByDoneDateDescending()
    {
        await using var db = CreateDb();
        var project = MakeProject();
        var person = MakePerson();
        db.Projects.Add(project);
        db.Persons.Add(person);
        await db.SaveChangesAsync();

        var wi1 = MakeWorkItem(project.Id, status: WorkItemStatus.Done);
        var wi2 = MakeWorkItem(project.Id, status: WorkItemStatus.Done);
        db.WorkItems.AddRange(wi1, wi2);
        await db.SaveChangesAsync();

        // wi1 Done más tarde
        var early = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var late  = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);

        db.WorkItemStatusHistories.Add(WorkItemStatusHistory.Create(wi1, null, WorkItemStatus.Backlog, person.Id, early));
        db.WorkItemStatusHistories.Add(WorkItemStatusHistory.Create(wi1, WorkItemStatus.Backlog, WorkItemStatus.Done, person.Id, late));

        db.WorkItemStatusHistories.Add(WorkItemStatusHistory.Create(wi2, null, WorkItemStatus.Backlog, person.Id, early));
        db.WorkItemStatusHistories.Add(WorkItemStatusHistory.Create(wi2, WorkItemStatus.Backlog, WorkItemStatus.Done, person.Id, early));

        await db.SaveChangesAsync();

        var handler = new GetProjectCycleTimeHandler(db);
        var result = await handler.Handle(new GetProjectCycleTimeQuery(project.Id), CancellationToken.None);

        // wi1 tiene Done más tardío → debe aparecer primero
        result.Items[0].WorkItemId.ShouldBe(wi1.Id);
        result.Items[1].WorkItemId.ShouldBe(wi2.Id);
    }
}
