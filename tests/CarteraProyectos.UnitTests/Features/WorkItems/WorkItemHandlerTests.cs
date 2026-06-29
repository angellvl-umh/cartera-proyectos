using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Features.WorkItems;
using CarteraProyectos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace CarteraProyectos.UnitTests.Features.WorkItems;

public class WorkItemHandlerTests
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

    private static async Task<(AppDbContext db, Project project, Epic epic)> DbWithProjectAndEpic()
    {
        var (db, project) = await DbWithProject();
        var epic = Epic.Create(project.Id, "Épica", null, 0, 0);
        db.Epics.Add(epic);
        await db.SaveChangesAsync();
        return (db, project, epic);
    }

    private static WorkItem MakeWorkItem(int projectId, string title = "Tarea", int? epicId = null)
        => WorkItem.Create(projectId, title, null, WorkItemPriority.Medium, epicId, 0, null, false, null, null);

    // --- CreateWorkItem ---

    [Fact]
    public async Task CreateWorkItem_ValidCommand_CreatesAndReturnsId()
    {
        var (db, project) = await DbWithProject();
        var handler = new CreateWorkItemHandler(db);

        var id = await handler.Handle(
            new CreateWorkItemCommand(project.Id, "Tarea 1", null, WorkItemPriority.Medium, null, [], 0, null, false, null, null),
            CancellationToken.None);

        id.ShouldBeGreaterThan(0);
        var wi = await db.WorkItems.FindAsync(id);
        wi.ShouldNotBeNull();
        wi.Title.ShouldBe("Tarea 1");
        wi.Status.ShouldBe(WorkItemStatus.Backlog);
    }

    [Fact]
    public async Task CreateWorkItem_ProjectNotFound_ThrowsKeyNotFoundException()
    {
        await using var db = CreateDb();
        var handler = new CreateWorkItemHandler(db);

        await Should.ThrowAsync<KeyNotFoundException>(
            () => handler.Handle(
                new CreateWorkItemCommand(999, "X", null, WorkItemPriority.Low, null, [], 0, null, false, null, null),
                CancellationToken.None));
    }

    [Fact]
    public async Task CreateWorkItem_EpicFromOtherProject_ThrowsKeyNotFoundException()
    {
        var (db, project) = await DbWithProject();
        var otherProject = Project.Create("Otro", null, "TIC", ProjectComplexity.VerySmall, null, null, null);
        db.Projects.Add(otherProject);
        var epic = Epic.Create(otherProject.Id, "Épica ajena", null, 0, 0);
        db.Epics.Add(epic);
        await db.SaveChangesAsync();

        var handler = new CreateWorkItemHandler(db);

        await Should.ThrowAsync<KeyNotFoundException>(
            () => handler.Handle(
                new CreateWorkItemCommand(project.Id, "Tarea", null, WorkItemPriority.Low, epic.Id, [], 0, null, false, null, null),
                CancellationToken.None));
    }

    [Fact]
    public async Task CreateWorkItem_WithValidEpic_AssignsEpicId()
    {
        var (db, project, epic) = await DbWithProjectAndEpic();
        var handler = new CreateWorkItemHandler(db);

        var id = await handler.Handle(
            new CreateWorkItemCommand(project.Id, "Tarea con épica", null, WorkItemPriority.High, epic.Id, [], 0, null, false, null, null),
            CancellationToken.None);

        var wi = await db.WorkItems.FindAsync(id);
        wi!.EpicId.ShouldBe(epic.Id);
    }

    [Fact]
    public async Task CreateWorkItem_WithAssignees_AttachesPersons()
    {
        var (db, project) = await DbWithProject();
        var person = Person.CreateFromClaims("sub-1", "Dev User", "dev@test.com", PersonRole.Desarrollador);
        db.Persons.Add(person);
        await db.SaveChangesAsync();

        var handler = new CreateWorkItemHandler(db);
        var id = await handler.Handle(
            new CreateWorkItemCommand(project.Id, "Tarea asignada", null, WorkItemPriority.Medium, null, [person.Id], 0, null, false, null, null),
            CancellationToken.None);

        var wi = await db.WorkItems.Include(w => w.Assignees).FirstAsync(w => w.Id == id);
        wi.Assignees.Count.ShouldBe(1);
        wi.Assignees.First().Id.ShouldBe(person.Id);
    }

    // --- UpdateWorkItem ---

    [Fact]
    public async Task UpdateWorkItem_ValidCommand_UpdatesFields()
    {
        var (db, project) = await DbWithProject();
        var wi = MakeWorkItem(project.Id);
        db.WorkItems.Add(wi);
        await db.SaveChangesAsync();

        var handler = new UpdateWorkItemHandler(db);
        await handler.Handle(
            new UpdateWorkItemCommand(wi.Id, "Actualizado", "Descripción", WorkItemPriority.High, null, [], 1, 4, false, null, new DateOnly(2026, 12, 31)),
            CancellationToken.None);

        var updated = await db.WorkItems.FindAsync(wi.Id);
        updated!.Title.ShouldBe("Actualizado");
        updated.Priority.ShouldBe(WorkItemPriority.High);
        updated.EstimationHours.ShouldBe(4);
        updated.DueDate.ShouldBe(new DateOnly(2026, 12, 31));
    }

    [Fact]
    public async Task UpdateWorkItem_NotFound_ThrowsKeyNotFoundException()
    {
        await using var db = CreateDb();
        var handler = new UpdateWorkItemHandler(db);

        await Should.ThrowAsync<KeyNotFoundException>(
            () => handler.Handle(
                new UpdateWorkItemCommand(999, "X", null, WorkItemPriority.Low, null, [], 0, null, false, null, null),
                CancellationToken.None));
    }

    // --- DeleteWorkItem ---

    [Fact]
    public async Task DeleteWorkItem_ExistingItem_RemovesIt()
    {
        var (db, project) = await DbWithProject();
        var wi = MakeWorkItem(project.Id);
        db.WorkItems.Add(wi);
        await db.SaveChangesAsync();

        var handler = new DeleteWorkItemHandler(db);
        await handler.Handle(new DeleteWorkItemCommand(wi.Id), CancellationToken.None);

        (await db.WorkItems.FindAsync(wi.Id)).ShouldBeNull();
    }

    [Fact]
    public async Task DeleteWorkItem_NotFound_ThrowsKeyNotFoundException()
    {
        await using var db = CreateDb();
        var handler = new DeleteWorkItemHandler(db);

        await Should.ThrowAsync<KeyNotFoundException>(
            () => handler.Handle(new DeleteWorkItemCommand(999), CancellationToken.None));
    }

    // --- TransitionWorkItemStatus ---

    [Fact]
    public async Task TransitionStatus_FromBacklogToInProgress_ChangesStatus()
    {
        var (db, project) = await DbWithProject();
        var wi = MakeWorkItem(project.Id);
        db.WorkItems.Add(wi);
        await db.SaveChangesAsync();

        var handler = new TransitionWorkItemStatusHandler(db);
        await handler.Handle(new TransitionWorkItemStatusCommand(wi.Id, WorkItemStatus.InProgress), CancellationToken.None);

        var updated = await db.WorkItems.FindAsync(wi.Id);
        updated!.Status.ShouldBe(WorkItemStatus.InProgress);
    }

    [Fact]
    public async Task TransitionStatus_ToBlocked_ChangesStatus()
    {
        var (db, project) = await DbWithProject();
        var wi = MakeWorkItem(project.Id);
        wi.TransitionStatus(WorkItemStatus.InProgress);
        db.WorkItems.Add(wi);
        await db.SaveChangesAsync();

        var handler = new TransitionWorkItemStatusHandler(db);
        await handler.Handle(new TransitionWorkItemStatusCommand(wi.Id, WorkItemStatus.Blocked), CancellationToken.None);

        var updated = await db.WorkItems.FindAsync(wi.Id);
        updated!.Status.ShouldBe(WorkItemStatus.Blocked);
    }

    [Fact]
    public async Task TransitionStatus_FromDone_ThrowsInvalidOperationException()
    {
        var (db, project) = await DbWithProject();
        var wi = MakeWorkItem(project.Id);
        wi.TransitionStatus(WorkItemStatus.Done);
        db.WorkItems.Add(wi);
        await db.SaveChangesAsync();

        var handler = new TransitionWorkItemStatusHandler(db);
        await Should.ThrowAsync<InvalidOperationException>(
            () => handler.Handle(new TransitionWorkItemStatusCommand(wi.Id, WorkItemStatus.InProgress), CancellationToken.None));
    }

    [Fact]
    public async Task TransitionStatus_NotFound_ThrowsKeyNotFoundException()
    {
        await using var db = CreateDb();
        var handler = new TransitionWorkItemStatusHandler(db);

        await Should.ThrowAsync<KeyNotFoundException>(
            () => handler.Handle(new TransitionWorkItemStatusCommand(999, WorkItemStatus.ToDo), CancellationToken.None));
    }

    // --- GetWorkItems ---

    [Fact]
    public async Task GetWorkItems_ReturnsItemsForProject()
    {
        var (db, project) = await DbWithProject();
        db.WorkItems.Add(MakeWorkItem(project.Id, "T1"));
        db.WorkItems.Add(MakeWorkItem(project.Id, "T2"));
        await db.SaveChangesAsync();

        var handler = new GetWorkItemsHandler(db);
        var result = await handler.Handle(new GetWorkItemsQuery(project.Id, null, null, 1, 10), CancellationToken.None);

        result.Total.ShouldBe(2);
        result.Items.Count.ShouldBe(2);
    }

    [Fact]
    public async Task GetWorkItems_FilterByEpic_ReturnsOnlyMatchingItems()
    {
        var (db, project, epic) = await DbWithProjectAndEpic();
        db.WorkItems.Add(MakeWorkItem(project.Id, "Con épica", epic.Id));
        db.WorkItems.Add(MakeWorkItem(project.Id, "Sin épica"));
        await db.SaveChangesAsync();

        var handler = new GetWorkItemsHandler(db);
        var result = await handler.Handle(new GetWorkItemsQuery(project.Id, epic.Id, null, 1, 10), CancellationToken.None);

        result.Total.ShouldBe(1);
        result.Items[0].Title.ShouldBe("Con épica");
    }

    // --- WorkItemType ---

    [Fact]
    public async Task CreateWorkItem_WithUserStoryType_PersistsType()
    {
        var (db, project) = await DbWithProject();
        var handler = new CreateWorkItemHandler(db);

        var id = await handler.Handle(
            new CreateWorkItemCommand(project.Id, "Historia de usuario", null, WorkItemPriority.Medium, null, [], 0, null, false, null, null, Type: WorkItemType.UserStory),
            CancellationToken.None);

        var wi = await db.WorkItems.FindAsync(id);
        wi!.Type.ShouldBe(WorkItemType.UserStory);
    }

    [Fact]
    public async Task CreateWorkItem_DefaultType_IsTask()
    {
        var (db, project) = await DbWithProject();
        var handler = new CreateWorkItemHandler(db);

        var id = await handler.Handle(
            new CreateWorkItemCommand(project.Id, "Tarea por defecto", null, WorkItemPriority.Medium, null, [], 0, null, false, null, null),
            CancellationToken.None);

        var wi = await db.WorkItems.FindAsync(id);
        wi!.Type.ShouldBe(WorkItemType.Task);
    }

    // --- WorkItemStatusHistory ---

    [Fact]
    public async Task CreateWorkItem_RecordsInitialStatusHistory()
    {
        var (db, project) = await DbWithProject();
        var creator = Person.CreateFromClaims("sub-creator", "Creador", "creador@test.com", PersonRole.Gestor);
        db.Persons.Add(creator);
        await db.SaveChangesAsync();

        var handler = new CreateWorkItemHandler(db);
        var id = await handler.Handle(
            new CreateWorkItemCommand(project.Id, "Tarea", null, WorkItemPriority.Medium, null, [], 0, null, false, null, null,
                RequestingPersonId: creator.Id),
            CancellationToken.None);

        var history = await db.WorkItemStatusHistories.Where(h => h.WorkItemId == id).ToListAsync();
        history.Count.ShouldBe(1);
        history[0].FromStatus.ShouldBeNull();
        history[0].ToStatus.ShouldBe(WorkItemStatus.Backlog);
        history[0].ChangedById.ShouldBe(creator.Id);
    }

    [Fact]
    public async Task TransitionStatus_RecordsHistoryEntry()
    {
        var (db, project) = await DbWithProject();
        var gestor = Person.CreateFromClaims("sub-gestor", "Gestor", "gestor2@test.com", PersonRole.Gestor);
        db.Persons.Add(gestor);
        var wi = MakeWorkItem(project.Id);
        db.WorkItems.Add(wi);
        await db.SaveChangesAsync();

        var handler = new TransitionWorkItemStatusHandler(db);
        await handler.Handle(new TransitionWorkItemStatusCommand(wi.Id, WorkItemStatus.InProgress, gestor.Id), CancellationToken.None);

        var history = await db.WorkItemStatusHistories.Where(h => h.WorkItemId == wi.Id).ToListAsync();
        history.Count.ShouldBe(1);
        history[0].FromStatus.ShouldBe(WorkItemStatus.Backlog);
        history[0].ToStatus.ShouldBe(WorkItemStatus.InProgress);
        history[0].ChangedById.ShouldBe(gestor.Id);
    }

    [Fact]
    public async Task TransitionStatus_FromDone_DoesNotRecordHistory()
    {
        var (db, project) = await DbWithProject();
        var wi = MakeWorkItem(project.Id);
        wi.TransitionStatus(WorkItemStatus.Done);
        db.WorkItems.Add(wi);
        await db.SaveChangesAsync();

        var handler = new TransitionWorkItemStatusHandler(db);
        await Should.ThrowAsync<InvalidOperationException>(
            () => handler.Handle(new TransitionWorkItemStatusCommand(wi.Id, WorkItemStatus.InProgress), CancellationToken.None));

        var history = await db.WorkItemStatusHistories.Where(h => h.WorkItemId == wi.Id).ToListAsync();
        history.ShouldBeEmpty();
    }

    // --- GetWorkItemStatusHistory ---

    [Fact]
    public async Task GetWorkItemStatusHistory_ReturnsEntriesOrderedByDate()
    {
        var (db, project) = await DbWithProject();
        var person = Person.CreateFromClaims("sub-h1", "Historiador", "hist@test.com", PersonRole.Gestor);
        db.Persons.Add(person);
        var wi = MakeWorkItem(project.Id);
        db.WorkItems.Add(wi);
        await db.SaveChangesAsync();

        var transitionHandler = new TransitionWorkItemStatusHandler(db);
        await transitionHandler.Handle(new TransitionWorkItemStatusCommand(wi.Id, WorkItemStatus.ToDo, person.Id), CancellationToken.None);
        await transitionHandler.Handle(new TransitionWorkItemStatusCommand(wi.Id, WorkItemStatus.InProgress, person.Id), CancellationToken.None);

        var handler = new GetWorkItemStatusHistoryHandler(db);
        var result = await handler.Handle(new GetWorkItemStatusHistoryQuery(project.Id, wi.Id), CancellationToken.None);

        result.Count.ShouldBe(2);
        result[0].ToStatus.ShouldBe("ToDo");
        result[1].ToStatus.ShouldBe("InProgress");
        result[1].ChangedByName.ShouldBe("Historiador");
    }

    [Fact]
    public async Task GetWorkItemStatusHistory_WorkItemNotFound_ThrowsKeyNotFoundException()
    {
        await using var db = CreateDb();
        var handler = new GetWorkItemStatusHistoryHandler(db);

        await Should.ThrowAsync<KeyNotFoundException>(
            () => handler.Handle(new GetWorkItemStatusHistoryQuery(1, 999), CancellationToken.None));
    }
}
