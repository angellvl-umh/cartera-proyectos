using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Features.Agent;
using CarteraProyectos.Core.Features.WorkItems;
using CarteraProyectos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace CarteraProyectos.UnitTests.Features.Agent;

public class AgentBacklogHandlerTests
{
    // ── Infraestructura ───────────────────────────────────────────────────────

    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static async Task<Person> AddPersonAsync(AppDbContext db, PersonRole role = PersonRole.Gestor)
    {
        var person = Person.CreateFromClaims(Guid.NewGuid().ToString(),
            role.ToString(), $"{role.ToString().ToLower()}@uni.es", role);
        db.Persons.Add(person);
        await db.SaveChangesAsync();
        return person;
    }

    private static async Task<Project> AddProjectAsync(AppDbContext db, string title = "Proyecto Test")
    {
        var project = Project.Create(title, null, "TIC", ProjectComplexity.Small, null, null, null);
        db.Projects.Add(project);
        await db.SaveChangesAsync();
        return project;
    }

    private static WorkItem MakeWorkItem(int projectId, string title = "Tarea", int sortOrder = 0)
        => WorkItem.Create(projectId, title, null, WorkItemPriority.Medium, null, sortOrder, null, false, null, null);

    // ══════════════════════════════════════════════════════════════════════════
    // AgentReorderBacklogHandler
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task AgentReorderBacklog_HappyPath_ReasignaSortOrder()
    {
        await using var db = CreateDb();
        var person = await AddPersonAsync(db);
        var project = await AddProjectAsync(db);

        var w1 = MakeWorkItem(project.Id, "T1", sortOrder: 30);
        var w2 = MakeWorkItem(project.Id, "T2", sortOrder: 20);
        var w3 = MakeWorkItem(project.Id, "T3", sortOrder: 10);
        db.WorkItems.AddRange(w1, w2, w3);
        await db.SaveChangesAsync();

        var handler = new AgentReorderBacklogHandler(new WorkItemLifecycleService(db));
        // Queremos el orden: w3, w1, w2 → sortOrders 10, 20, 30
        await handler.Handle(
            new AgentReorderBacklogCommand(person.Id, project.Id, [w3.Id, w1.Id, w2.Id]),
            CancellationToken.None);

        var updated = await db.WorkItems.ToListAsync();
        updated.First(w => w.Id == w3.Id).SortOrder.ShouldBe(10);
        updated.First(w => w.Id == w1.Id).SortOrder.ShouldBe(20);
        updated.First(w => w.Id == w2.Id).SortOrder.ShouldBe(30);
    }

    [Fact]
    public async Task AgentReorderBacklog_RequestingPersonId_EsElPersonId()
    {
        var cmd = new AgentReorderBacklogCommand(99, 1, []);
        cmd.RequestingPersonId.ShouldBe(99);
    }

    [Fact]
    public async Task AgentReorderBacklog_IdDeOtroProyecto_PropagaKeyNotFound()
    {
        await using var db = CreateDb();
        var person = await AddPersonAsync(db);
        var project = await AddProjectAsync(db);
        var otherProject = Project.Create("Otro", null, "TIC", ProjectComplexity.VerySmall, null, null, null);
        db.Projects.Add(otherProject);
        var w1 = MakeWorkItem(project.Id, "T1");
        var wOther = MakeWorkItem(otherProject.Id, "Ajena");
        db.WorkItems.AddRange(w1, wOther);
        await db.SaveChangesAsync();

        var handler = new AgentReorderBacklogHandler(new WorkItemLifecycleService(db));

        await Should.ThrowAsync<KeyNotFoundException>(() =>
            handler.Handle(
                new AgentReorderBacklogCommand(person.Id, project.Id, [w1.Id, wOther.Id]),
                CancellationToken.None));
    }

    [Fact]
    public async Task AgentReorderBacklog_IdInexistente_PropagaKeyNotFound()
    {
        await using var db = CreateDb();
        var person = await AddPersonAsync(db);
        var project = await AddProjectAsync(db);
        var w1 = MakeWorkItem(project.Id, "T1");
        db.WorkItems.Add(w1);
        await db.SaveChangesAsync();

        var handler = new AgentReorderBacklogHandler(new WorkItemLifecycleService(db));

        await Should.ThrowAsync<KeyNotFoundException>(() =>
            handler.Handle(
                new AgentReorderBacklogCommand(person.Id, project.Id, [w1.Id, 99999]),
                CancellationToken.None));
    }

    // ══════════════════════════════════════════════════════════════════════════
    // AgentBulkAssignToSprintHandler
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task AgentBulkAssignToSprint_HappyPath_AsignaTareas()
    {
        await using var db = CreateDb();
        var person = await AddPersonAsync(db);
        var project = await AddProjectAsync(db);
        var sprint = Sprint.Create(project.Id, "Sprint 1", null, null, null, null);
        db.Sprints.Add(sprint);
        var w1 = MakeWorkItem(project.Id, "T1");
        var w2 = MakeWorkItem(project.Id, "T2");
        db.WorkItems.AddRange(w1, w2);
        await db.SaveChangesAsync();

        var handler = new AgentBulkAssignToSprintHandler(new WorkItemLifecycleService(db));
        await handler.Handle(
            new AgentBulkAssignToSprintCommand(person.Id, project.Id, [w1.Id, w2.Id], sprint.Id),
            CancellationToken.None);

        var updated = await db.WorkItems.ToListAsync();
        updated.First(w => w.Id == w1.Id).SprintId.ShouldBe(sprint.Id);
        updated.First(w => w.Id == w2.Id).SprintId.ShouldBe(sprint.Id);
    }

    [Fact]
    public async Task AgentBulkAssignToSprint_NullSprintId_MueveABacklog()
    {
        await using var db = CreateDb();
        var person = await AddPersonAsync(db);
        var project = await AddProjectAsync(db);
        var sprint = Sprint.Create(project.Id, "Sprint 1", null, null, null, null);
        db.Sprints.Add(sprint);
        var w1 = MakeWorkItem(project.Id, "T1");
        db.WorkItems.Add(w1);
        await db.SaveChangesAsync();

        w1.AssignToSprint(sprint.Id);
        await db.SaveChangesAsync();

        var handler = new AgentBulkAssignToSprintHandler(new WorkItemLifecycleService(db));
        await handler.Handle(
            new AgentBulkAssignToSprintCommand(person.Id, project.Id, [w1.Id], null),
            CancellationToken.None);

        var updated = await db.WorkItems.FindAsync(w1.Id);
        updated!.SprintId.ShouldBeNull();
    }

    [Fact]
    public async Task AgentBulkAssignToSprint_RequestingPersonId_EsElPersonId()
    {
        var cmd = new AgentBulkAssignToSprintCommand(88, 1, [], null);
        cmd.RequestingPersonId.ShouldBe(88);
    }

    [Fact]
    public async Task AgentBulkAssignToSprint_SprintDeOtroProyecto_PropagaKeyNotFound()
    {
        await using var db = CreateDb();
        var person = await AddPersonAsync(db);
        var project = await AddProjectAsync(db);
        var otherProject = Project.Create("Otro", null, "TIC", ProjectComplexity.VerySmall, null, null, null);
        db.Projects.Add(otherProject);
        var sprintOther = Sprint.Create(otherProject.Id, "Sprint ajeno", null, null, null, null);
        db.Sprints.Add(sprintOther);
        var w1 = MakeWorkItem(project.Id, "T1");
        db.WorkItems.Add(w1);
        await db.SaveChangesAsync();

        var handler = new AgentBulkAssignToSprintHandler(new WorkItemLifecycleService(db));

        await Should.ThrowAsync<KeyNotFoundException>(() =>
            handler.Handle(
                new AgentBulkAssignToSprintCommand(person.Id, project.Id, [w1.Id], sprintOther.Id),
                CancellationToken.None));
    }
}
