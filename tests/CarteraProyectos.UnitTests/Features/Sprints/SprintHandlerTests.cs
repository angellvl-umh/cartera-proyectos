using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Features.Sprints;
using CarteraProyectos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace CarteraProyectos.UnitTests.Features.Sprints;

public class SprintHandlerTests
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

    private static Sprint MakeSprint(int projectId, string name = "Sprint 1", SprintStatus status = SprintStatus.Planning)
    {
        var sprint = Sprint.Create(projectId, name, null, null, null, null);
        if (status == SprintStatus.Active) sprint.TransitionStatus(SprintStatus.Active);
        if (status == SprintStatus.Completed)
        {
            sprint.TransitionStatus(SprintStatus.Active);
            sprint.TransitionStatus(SprintStatus.Completed);
        }
        return sprint;
    }

    // --- CreateSprint ---

    [Fact]
    public async Task CreateSprint_ValidData_CreatesAndReturnsId()
    {
        var (db, project) = await DbWithProject();
        var handler = new CreateSprintHandler(db);

        var id = await handler.Handle(
            new CreateSprintCommand(project.Id, "Sprint 1", "Objetivo del sprint",
                new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 14), 80),
            CancellationToken.None);

        id.ShouldBeGreaterThan(0);
        var sprint = await db.Sprints.FindAsync(id);
        sprint.ShouldNotBeNull();
        sprint.Name.ShouldBe("Sprint 1");
        sprint.Status.ShouldBe(SprintStatus.Planning);
        sprint.Capacity.ShouldBe(80);
    }

    [Fact]
    public async Task CreateSprint_ProjectNotFound_ThrowsKeyNotFoundException()
    {
        await using var db = CreateDb();
        var handler = new CreateSprintHandler(db);

        await Should.ThrowAsync<KeyNotFoundException>(
            () => handler.Handle(
                new CreateSprintCommand(999, "Sprint X", null, null, null, null),
                CancellationToken.None));
    }

    // --- UpdateSprint ---

    [Fact]
    public async Task UpdateSprint_PlanningStatus_UpdatesFields()
    {
        var (db, project) = await DbWithProject();
        var sprint = MakeSprint(project.Id);
        db.Sprints.Add(sprint);
        await db.SaveChangesAsync();

        var handler = new UpdateSprintHandler(db);
        await handler.Handle(
            new UpdateSprintCommand(sprint.Id, "Sprint Actualizado", "Nuevo objetivo",
                new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 14), 100),
            CancellationToken.None);

        var updated = await db.Sprints.FindAsync(sprint.Id);
        updated!.Name.ShouldBe("Sprint Actualizado");
        updated.Capacity.ShouldBe(100);
    }

    [Fact]
    public async Task UpdateSprint_ActiveStatus_ThrowsInvalidOperationException()
    {
        var (db, project) = await DbWithProject();
        var sprint = MakeSprint(project.Id, status: SprintStatus.Active);
        db.Sprints.Add(sprint);
        await db.SaveChangesAsync();

        var handler = new UpdateSprintHandler(db);
        await Should.ThrowAsync<InvalidOperationException>(
            () => handler.Handle(
                new UpdateSprintCommand(sprint.Id, "Nombre", null, null, null, null),
                CancellationToken.None));
    }

    // --- DeleteSprint ---

    [Fact]
    public async Task DeleteSprint_PlanningStatus_RemovesSprint()
    {
        var (db, project) = await DbWithProject();
        var sprint = MakeSprint(project.Id);
        db.Sprints.Add(sprint);
        await db.SaveChangesAsync();

        var handler = new DeleteSprintHandler(db);
        await handler.Handle(new DeleteSprintCommand(sprint.Id), CancellationToken.None);

        (await db.Sprints.FindAsync(sprint.Id)).ShouldBeNull();
    }

    [Fact]
    public async Task DeleteSprint_ActiveSprint_ThrowsInvalidOperationException()
    {
        var (db, project) = await DbWithProject();
        var sprint = MakeSprint(project.Id, status: SprintStatus.Active);
        db.Sprints.Add(sprint);
        await db.SaveChangesAsync();

        var handler = new DeleteSprintHandler(db);
        await Should.ThrowAsync<InvalidOperationException>(
            () => handler.Handle(new DeleteSprintCommand(sprint.Id), CancellationToken.None));
    }

    [Fact]
    public async Task DeleteSprint_NotFound_ThrowsKeyNotFoundException()
    {
        await using var db = CreateDb();
        var handler = new DeleteSprintHandler(db);

        await Should.ThrowAsync<KeyNotFoundException>(
            () => handler.Handle(new DeleteSprintCommand(999), CancellationToken.None));
    }

    [Fact]
    public async Task DeleteSprint_WithWorkItems_ThrowsInvalidOperationException()
    {
        var (db, project) = await DbWithProject();
        var sprint = MakeSprint(project.Id);
        db.Sprints.Add(sprint);
        await db.SaveChangesAsync();
        db.WorkItems.Add(WorkItem.Create(project.Id, "Tarea", null, WorkItemPriority.Medium, null, 0, null, false, null, null, sprint.Id));
        await db.SaveChangesAsync();

        var handler = new DeleteSprintHandler(db);

        await Should.ThrowAsync<InvalidOperationException>(
            () => handler.Handle(new DeleteSprintCommand(sprint.Id), CancellationToken.None));
    }

    // --- TransitionSprintStatus ---

    [Fact]
    public async Task TransitionStatus_PlanningToActive_Succeeds()
    {
        var (db, project) = await DbWithProject();
        var sprint = MakeSprint(project.Id);
        db.Sprints.Add(sprint);
        await db.SaveChangesAsync();

        var handler = new TransitionSprintStatusHandler(db);
        await handler.Handle(new TransitionSprintStatusCommand(sprint.Id, SprintStatus.Active), CancellationToken.None);

        var updated = await db.Sprints.FindAsync(sprint.Id);
        updated!.Status.ShouldBe(SprintStatus.Active);
    }

    [Fact]
    public async Task TransitionStatus_ActivateWhenAnotherActive_ThrowsInvalidOperationException()
    {
        var (db, project) = await DbWithProject();
        var active = MakeSprint(project.Id, "Sprint Activo", SprintStatus.Active);
        var planning = MakeSprint(project.Id, "Sprint Nuevo");
        db.Sprints.AddRange(active, planning);
        await db.SaveChangesAsync();

        var handler = new TransitionSprintStatusHandler(db);
        await Should.ThrowAsync<InvalidOperationException>(
            () => handler.Handle(new TransitionSprintStatusCommand(planning.Id, SprintStatus.Active), CancellationToken.None));
    }

    [Fact]
    public async Task TransitionStatus_ActiveToCompleted_Succeeds()
    {
        var (db, project) = await DbWithProject();
        var sprint = MakeSprint(project.Id, status: SprintStatus.Active);
        db.Sprints.Add(sprint);
        await db.SaveChangesAsync();

        var handler = new TransitionSprintStatusHandler(db);
        await handler.Handle(new TransitionSprintStatusCommand(sprint.Id, SprintStatus.Completed), CancellationToken.None);

        var updated = await db.Sprints.FindAsync(sprint.Id);
        updated!.Status.ShouldBe(SprintStatus.Completed);
    }

    [Fact]
    public async Task TransitionStatus_ActiveToCompleted_WithUnfinishedWorkItems_ThrowsInvalidOperationException()
    {
        var (db, project) = await DbWithProject();
        var sprint = MakeSprint(project.Id, status: SprintStatus.Active);
        db.Sprints.Add(sprint);
        await db.SaveChangesAsync();
        db.WorkItems.Add(WorkItem.Create(project.Id, "Tarea pendiente", null, WorkItemPriority.Medium, null, 0, null, false, null, null, sprint.Id));
        await db.SaveChangesAsync();

        var handler = new TransitionSprintStatusHandler(db);

        await Should.ThrowAsync<InvalidOperationException>(
            () => handler.Handle(new TransitionSprintStatusCommand(sprint.Id, SprintStatus.Completed), CancellationToken.None));
    }

    [Fact]
    public async Task TransitionStatus_CompletedToAny_ThrowsInvalidOperationException()
    {
        var (db, project) = await DbWithProject();
        var sprint = MakeSprint(project.Id, status: SprintStatus.Completed);
        db.Sprints.Add(sprint);
        await db.SaveChangesAsync();

        var handler = new TransitionSprintStatusHandler(db);
        await Should.ThrowAsync<InvalidOperationException>(
            () => handler.Handle(new TransitionSprintStatusCommand(sprint.Id, SprintStatus.Active), CancellationToken.None));
    }

    // --- GetSprints ---

    [Fact]
    public async Task GetSprints_ReturnsSprintsForProject()
    {
        var (db, project) = await DbWithProject();
        db.Sprints.Add(Sprint.Create(project.Id, "Sprint 1", null, new DateOnly(2026, 7, 1), null, null));
        db.Sprints.Add(Sprint.Create(project.Id, "Sprint 2", null, new DateOnly(2026, 8, 1), null, null));
        await db.SaveChangesAsync();

        var handler = new GetSprintsHandler(db);
        var result = await handler.Handle(new GetSprintsQuery(project.Id, 1, 10), CancellationToken.None);

        result.Total.ShouldBe(2);
        result.Items.Count.ShouldBe(2);
        result.Items[0].Name.ShouldBe("Sprint 2"); // ordered by StartDate desc
    }

    // --- SprintStatusHistory ---

    [Fact]
    public async Task CreateSprint_RecordsInitialStatusHistory()
    {
        var (db, project) = await DbWithProject();
        var creator = Person.CreateFromClaims("sub-creator", "Creador", "creador-sprint@test.com", PersonRole.Gestor);
        db.Persons.Add(creator);
        await db.SaveChangesAsync();

        var handler = new CreateSprintHandler(db);
        var id = await handler.Handle(
            new CreateSprintCommand(project.Id, "Sprint 1", null, null, null, null, RequestingPersonId: creator.Id),
            CancellationToken.None);

        var history = await db.SprintStatusHistories.Where(h => h.SprintId == id).ToListAsync();
        history.Count.ShouldBe(1);
        history[0].FromStatus.ShouldBeNull();
        history[0].ToStatus.ShouldBe(SprintStatus.Planning);
        history[0].ChangedById.ShouldBe(creator.Id);
    }

    [Fact]
    public async Task TransitionSprintStatus_RecordsHistoryEntry()
    {
        var (db, project) = await DbWithProject();
        var lead = Person.CreateFromClaims("sub-lead", "Lider", "lider@test.com", PersonRole.JefeEquipo);
        db.Persons.Add(lead);
        var sprint = MakeSprint(project.Id);
        db.Sprints.Add(sprint);
        await db.SaveChangesAsync();

        var handler = new TransitionSprintStatusHandler(db);
        await handler.Handle(new TransitionSprintStatusCommand(sprint.Id, SprintStatus.Active, lead.Id), CancellationToken.None);

        var history = await db.SprintStatusHistories.Where(h => h.SprintId == sprint.Id).ToListAsync();
        history.Count.ShouldBe(1);
        history[0].FromStatus.ShouldBe(SprintStatus.Planning);
        history[0].ToStatus.ShouldBe(SprintStatus.Active);
        history[0].ChangedById.ShouldBe(lead.Id);
    }

    [Fact]
    public async Task TransitionSprintStatus_InvalidTransition_DoesNotRecordHistory()
    {
        var (db, project) = await DbWithProject();
        var sprint = MakeSprint(project.Id, status: SprintStatus.Completed);
        db.Sprints.Add(sprint);
        await db.SaveChangesAsync();

        var handler = new TransitionSprintStatusHandler(db);
        await Should.ThrowAsync<InvalidOperationException>(
            () => handler.Handle(new TransitionSprintStatusCommand(sprint.Id, SprintStatus.Active), CancellationToken.None));

        var history = await db.SprintStatusHistories.Where(h => h.SprintId == sprint.Id).ToListAsync();
        history.ShouldBeEmpty();
    }

    // --- GetSprintStatusHistory ---

    [Fact]
    public async Task GetSprintStatusHistory_ReturnsEntriesOrderedByDate()
    {
        var (db, project) = await DbWithProject();
        var person = Person.CreateFromClaims("sub-h2", "Historiador", "hist-sprint@test.com", PersonRole.Gestor);
        db.Persons.Add(person);
        var sprint = MakeSprint(project.Id);
        db.Sprints.Add(sprint);
        await db.SaveChangesAsync();

        var transitionHandler = new TransitionSprintStatusHandler(db);
        await transitionHandler.Handle(new TransitionSprintStatusCommand(sprint.Id, SprintStatus.Active, person.Id), CancellationToken.None);
        await transitionHandler.Handle(new TransitionSprintStatusCommand(sprint.Id, SprintStatus.Completed, person.Id), CancellationToken.None);

        var handler = new GetSprintStatusHistoryHandler(db);
        var result = await handler.Handle(new GetSprintStatusHistoryQuery(project.Id, sprint.Id), CancellationToken.None);

        result.Count.ShouldBe(2);
        result[0].ToStatus.ShouldBe("Active");
        result[1].ToStatus.ShouldBe("Completed");
        result[1].ChangedByName.ShouldBe("Historiador");
    }

    [Fact]
    public async Task GetSprintStatusHistory_SprintNotFound_ThrowsKeyNotFoundException()
    {
        await using var db = CreateDb();
        var handler = new GetSprintStatusHistoryHandler(db);

        await Should.ThrowAsync<KeyNotFoundException>(
            () => handler.Handle(new GetSprintStatusHistoryQuery(1, 999), CancellationToken.None));
    }
}
