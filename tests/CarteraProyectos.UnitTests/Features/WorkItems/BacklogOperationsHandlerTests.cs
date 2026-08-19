using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Features.WorkItems;
using CarteraProyectos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace CarteraProyectos.UnitTests.Features.WorkItems;

/// <summary>
/// Tests para los nuevos filtros de GetWorkItems (Tarea 1),
/// ReorderWorkItems (Tarea 2) y BulkAssignWorkItemsToSprint (Tarea 3).
/// </summary>
public class BacklogOperationsHandlerTests
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

    private static WorkItem MakeWorkItem(
        int projectId,
        string title = "Tarea",
        WorkItemPriority priority = WorkItemPriority.Medium,
        WorkItemType type = WorkItemType.Task,
        int sortOrder = 0)
        => WorkItem.Create(projectId, title, null, priority, null, sortOrder, null, false, null, null, type: type);

    // ── GetWorkItems: filtro Q ────────────────────────────────────────────────

    [Fact]
    public async Task GetWorkItems_FilterQ_CaseInsensitive_ReturnsMatchingItems()
    {
        var (db, project) = await DbWithProject();
        db.WorkItems.Add(MakeWorkItem(project.Id, "Proxy inverso"));
        db.WorkItems.Add(MakeWorkItem(project.Id, "Certificado SSL"));
        db.WorkItems.Add(MakeWorkItem(project.Id, "proxy directo"));
        await db.SaveChangesAsync();

        var handler = new GetWorkItemsHandler(db);
        var result = await handler.Handle(
            new GetWorkItemsQuery(project.Id, Q: "proxy"),
            CancellationToken.None);

        result.Total.ShouldBe(2);
        result.Items.ShouldAllBe(i => i.Title.ToLower().Contains("proxy"));
    }

    [Fact]
    public async Task GetWorkItems_FilterQ_NoMatch_ReturnsEmpty()
    {
        var (db, project) = await DbWithProject();
        db.WorkItems.Add(MakeWorkItem(project.Id, "Tarea A"));
        await db.SaveChangesAsync();

        var handler = new GetWorkItemsHandler(db);
        var result = await handler.Handle(
            new GetWorkItemsQuery(project.Id, Q: "inexistente"),
            CancellationToken.None);

        result.Total.ShouldBe(0);
    }

    // ── GetWorkItems: filtro AssigneeId ───────────────────────────────────────

    [Fact]
    public async Task GetWorkItems_FilterAssigneeId_ReturnsOnlyAssignedItems()
    {
        var (db, project) = await DbWithProject();
        var person = Person.CreateFromClaims("sub-1", "Dev", "dev@test.com", PersonRole.Desarrollador);
        db.Persons.Add(person);
        await db.SaveChangesAsync();

        // Tarea asignada al dev
        var assigned = WorkItem.Create(project.Id, "Asignada", null, WorkItemPriority.Medium, null, 0, null, false, null, null);
        var unassigned = WorkItem.Create(project.Id, "Sin asignar", null, WorkItemPriority.Medium, null, 0, null, false, null, null);
        db.WorkItems.Add(assigned);
        db.WorkItems.Add(unassigned);
        await db.SaveChangesAsync();

        assigned.AddAssignee(person);
        await db.SaveChangesAsync();

        var handler = new GetWorkItemsHandler(db);
        var result = await handler.Handle(
            new GetWorkItemsQuery(project.Id, AssigneeId: person.Id),
            CancellationToken.None);

        result.Total.ShouldBe(1);
        result.Items[0].Title.ShouldBe("Asignada");
    }

    // ── GetWorkItems: filtro Priority ─────────────────────────────────────────

    [Fact]
    public async Task GetWorkItems_FilterPriority_ReturnsOnlyMatchingPriority()
    {
        var (db, project) = await DbWithProject();
        db.WorkItems.Add(MakeWorkItem(project.Id, "Alta prioridad", WorkItemPriority.High));
        db.WorkItems.Add(MakeWorkItem(project.Id, "Media prioridad", WorkItemPriority.Medium));
        db.WorkItems.Add(MakeWorkItem(project.Id, "Crítica", WorkItemPriority.Critical));
        await db.SaveChangesAsync();

        var handler = new GetWorkItemsHandler(db);
        var result = await handler.Handle(
            new GetWorkItemsQuery(project.Id, Priority: WorkItemPriority.High),
            CancellationToken.None);

        result.Total.ShouldBe(1);
        result.Items[0].Priority.ShouldBe("High");
    }

    // ── GetWorkItems: filtro Type ─────────────────────────────────────────────

    [Fact]
    public async Task GetWorkItems_FilterType_ReturnsOnlyMatchingType()
    {
        var (db, project) = await DbWithProject();
        db.WorkItems.Add(MakeWorkItem(project.Id, "Tarea normal", type: WorkItemType.Task));
        db.WorkItems.Add(MakeWorkItem(project.Id, "Historia de usuario", type: WorkItemType.UserStory));
        await db.SaveChangesAsync();

        var handler = new GetWorkItemsHandler(db);
        var result = await handler.Handle(
            new GetWorkItemsQuery(project.Id, Type: WorkItemType.UserStory),
            CancellationToken.None);

        result.Total.ShouldBe(1);
        result.Items[0].Title.ShouldBe("Historia de usuario");
    }

    // ── GetWorkItems: filtros combinados ──────────────────────────────────────

    [Fact]
    public async Task GetWorkItems_CombinedFilters_QAndPriority_ReturnsOnlyMatching()
    {
        var (db, project) = await DbWithProject();
        db.WorkItems.Add(MakeWorkItem(project.Id, "Proxy alta", WorkItemPriority.High));
        db.WorkItems.Add(MakeWorkItem(project.Id, "Proxy baja", WorkItemPriority.Low));
        db.WorkItems.Add(MakeWorkItem(project.Id, "SSL alta", WorkItemPriority.High));
        await db.SaveChangesAsync();

        var handler = new GetWorkItemsHandler(db);
        var result = await handler.Handle(
            new GetWorkItemsQuery(project.Id, Q: "proxy", Priority: WorkItemPriority.High),
            CancellationToken.None);

        result.Total.ShouldBe(1);
        result.Items[0].Title.ShouldBe("Proxy alta");
    }

    // ── ReorderWorkItems ──────────────────────────────────────────────────────

    [Fact]
    public async Task ReorderWorkItems_ValidIds_ReasignsSortOrderInMultiplesOf10()
    {
        var (db, project) = await DbWithProject();
        var w1 = MakeWorkItem(project.Id, "T1", sortOrder: 30);
        var w2 = MakeWorkItem(project.Id, "T2", sortOrder: 20);
        var w3 = MakeWorkItem(project.Id, "T3", sortOrder: 10);
        db.WorkItems.AddRange(w1, w2, w3);
        await db.SaveChangesAsync();

        var handler = new ReorderWorkItemsHandler(new WorkItemLifecycleService(db));
        // Queremos el orden: w3, w1, w2 → 10, 20, 30
        await handler.Handle(
            new ReorderWorkItemsCommand(project.Id, [w3.Id, w1.Id, w2.Id]),
            CancellationToken.None);

        var updated = await db.WorkItems.ToListAsync();
        updated.First(w => w.Id == w3.Id).SortOrder.ShouldBe(10);
        updated.First(w => w.Id == w1.Id).SortOrder.ShouldBe(20);
        updated.First(w => w.Id == w2.Id).SortOrder.ShouldBe(30);
    }

    [Fact]
    public async Task ReorderWorkItems_IdFromOtherProject_ThrowsKeyNotFoundException()
    {
        var (db, project) = await DbWithProject();
        var otherProject = Project.Create("Otro", null, "TIC", ProjectComplexity.VerySmall, null, null, null);
        db.Projects.Add(otherProject);
        var w1 = MakeWorkItem(project.Id, "T1");
        var wOther = MakeWorkItem(otherProject.Id, "Ajena");
        db.WorkItems.AddRange(w1, wOther);
        await db.SaveChangesAsync();

        var handler = new ReorderWorkItemsHandler(new WorkItemLifecycleService(db));

        await Should.ThrowAsync<KeyNotFoundException>(
            () => handler.Handle(
                new ReorderWorkItemsCommand(project.Id, [w1.Id, wOther.Id]),
                CancellationToken.None));
    }

    [Fact]
    public async Task ReorderWorkItems_NonExistentId_ThrowsKeyNotFoundException()
    {
        var (db, project) = await DbWithProject();
        var w1 = MakeWorkItem(project.Id, "T1");
        db.WorkItems.Add(w1);
        await db.SaveChangesAsync();

        var handler = new ReorderWorkItemsHandler(new WorkItemLifecycleService(db));

        await Should.ThrowAsync<KeyNotFoundException>(
            () => handler.Handle(
                new ReorderWorkItemsCommand(project.Id, [w1.Id, 99999]),
                CancellationToken.None));
    }

    // ── BulkAssignWorkItemsToSprint ───────────────────────────────────────────

    private static Sprint MakeSprint(int projectId, string name = "Sprint 1")
        => Sprint.Create(projectId, name, null, null, null, null);

    [Fact]
    public async Task BulkAssignToSprint_HappyPath_AssignsAllItems()
    {
        var (db, project) = await DbWithProject();
        var sprint = MakeSprint(project.Id);
        db.Sprints.Add(sprint);
        var w1 = MakeWorkItem(project.Id, "T1");
        var w2 = MakeWorkItem(project.Id, "T2");
        db.WorkItems.AddRange(w1, w2);
        await db.SaveChangesAsync();

        var handler = new BulkAssignWorkItemsToSprintHandler(new WorkItemLifecycleService(db));
        await handler.Handle(
            new BulkAssignWorkItemsToSprintCommand(project.Id, [w1.Id, w2.Id], sprint.Id),
            CancellationToken.None);

        var updated = await db.WorkItems.ToListAsync();
        updated.First(w => w.Id == w1.Id).SprintId.ShouldBe(sprint.Id);
        updated.First(w => w.Id == w2.Id).SprintId.ShouldBe(sprint.Id);
    }

    [Fact]
    public async Task BulkAssignToSprint_NullSprintId_MovesToBacklog()
    {
        var (db, project) = await DbWithProject();
        var sprint = MakeSprint(project.Id);
        db.Sprints.Add(sprint);
        var w1 = MakeWorkItem(project.Id, "T1");
        var w2 = MakeWorkItem(project.Id, "T2");
        db.WorkItems.AddRange(w1, w2);
        await db.SaveChangesAsync();

        // Primero asignamos al sprint
        w1.AssignToSprint(sprint.Id);
        w2.AssignToSprint(sprint.Id);
        await db.SaveChangesAsync();

        var handler = new BulkAssignWorkItemsToSprintHandler(new WorkItemLifecycleService(db));
        await handler.Handle(
            new BulkAssignWorkItemsToSprintCommand(project.Id, [w1.Id, w2.Id], null),
            CancellationToken.None);

        var updated = await db.WorkItems.ToListAsync();
        updated.First(w => w.Id == w1.Id).SprintId.ShouldBeNull();
        updated.First(w => w.Id == w2.Id).SprintId.ShouldBeNull();
    }

    [Fact]
    public async Task BulkAssignToSprint_SprintFromOtherProject_ThrowsKeyNotFoundException()
    {
        var (db, project) = await DbWithProject();
        var otherProject = Project.Create("Otro", null, "TIC", ProjectComplexity.VerySmall, null, null, null);
        db.Projects.Add(otherProject);
        var sprintOther = MakeSprint(otherProject.Id);
        db.Sprints.Add(sprintOther);
        var w1 = MakeWorkItem(project.Id, "T1");
        db.WorkItems.Add(w1);
        await db.SaveChangesAsync();

        var handler = new BulkAssignWorkItemsToSprintHandler(new WorkItemLifecycleService(db));

        await Should.ThrowAsync<KeyNotFoundException>(
            () => handler.Handle(
                new BulkAssignWorkItemsToSprintCommand(project.Id, [w1.Id], sprintOther.Id),
                CancellationToken.None));
    }

    [Fact]
    public async Task BulkAssignToSprint_ItemFromOtherProject_ThrowsKeyNotFoundException()
    {
        var (db, project) = await DbWithProject();
        var otherProject = Project.Create("Otro", null, "TIC", ProjectComplexity.VerySmall, null, null, null);
        db.Projects.Add(otherProject);
        var sprint = MakeSprint(project.Id);
        db.Sprints.Add(sprint);
        var w1 = MakeWorkItem(project.Id, "T1");
        var wOther = MakeWorkItem(otherProject.Id, "Ajena");
        db.WorkItems.AddRange(w1, wOther);
        await db.SaveChangesAsync();

        var handler = new BulkAssignWorkItemsToSprintHandler(new WorkItemLifecycleService(db));

        await Should.ThrowAsync<KeyNotFoundException>(
            () => handler.Handle(
                new BulkAssignWorkItemsToSprintCommand(project.Id, [w1.Id, wOther.Id], sprint.Id),
                CancellationToken.None));
    }
}
