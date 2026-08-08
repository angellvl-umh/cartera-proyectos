using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Features.Agent;
using CarteraProyectos.Core.Interfaces;
using CarteraProyectos.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace CarteraProyectos.UnitTests.Features.Agent;

public class AgentSprintsEpicsHandlerTests
{
    // ── Infraestructura ───────────────────────────────────────────────────────

    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static ISender CreateSender(AppDbContext db)
    {
        var services = new ServiceCollection();
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(AgentCreateSprintCommand).Assembly));
        services.AddScoped<IAppDbContext>(sp => db);
        return services.BuildServiceProvider().GetRequiredService<ISender>();
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

    private static async Task<Sprint> AddActiveSprint(AppDbContext db, int projectId, string name = "Sprint 1")
    {
        var sprint = Sprint.Create(projectId, name, null, null, null, null);
        sprint.TransitionStatus(SprintStatus.Active);
        db.Sprints.Add(sprint);
        await db.SaveChangesAsync();
        return sprint;
    }

    private static async Task<Epic> AddEpicAsync(AppDbContext db, int projectId, string title = "Épica Test")
    {
        var epic = Epic.Create(projectId, title, null, 1, 0);
        db.Epics.Add(epic);
        await db.SaveChangesAsync();
        return epic;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // AgentCreateSprintHandler
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task AgentCreateSprint_HappyPath_CreaSprintYDevuelveId()
    {
        await using var db = CreateDb();
        var person = await AddPersonAsync(db);
        var project = await AddProjectAsync(db);

        var handler = new AgentCreateSprintHandler(CreateSender(db));
        var id = await handler.Handle(
            new AgentCreateSprintCommand(
                person.Id, project.Id, "Sprint Alpha", "Objetivo",
                new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 14), 80),
            CancellationToken.None);

        id.ShouldBeGreaterThan(0);
        var sprint = await db.Sprints.FindAsync(id);
        sprint.ShouldNotBeNull();
        sprint.Name.ShouldBe("Sprint Alpha");
        sprint.Goal.ShouldBe("Objetivo");
        sprint.Capacity.ShouldBe(80);
        sprint.Status.ShouldBe(SprintStatus.Planning);
    }

    [Fact]
    public async Task AgentCreateSprint_RequestingPersonId_EsElPersonId()
    {
        await using var db = CreateDb();
        var person = await AddPersonAsync(db);
        var project = await AddProjectAsync(db);

        var cmd = new AgentCreateSprintCommand(person.Id, project.Id, "Sprint", null, null, null, null);
        cmd.RequestingPersonId.ShouldBe(person.Id);
    }

    [Fact]
    public async Task AgentCreateSprint_ProyectoInexistente_LanzaKeyNotFound()
    {
        await using var db = CreateDb();
        var person = await AddPersonAsync(db);

        var handler = new AgentCreateSprintHandler(CreateSender(db));
        await Should.ThrowAsync<KeyNotFoundException>(() =>
            handler.Handle(
                new AgentCreateSprintCommand(person.Id, 99999, "Sprint", null, null, null, null),
                CancellationToken.None));
    }

    // ══════════════════════════════════════════════════════════════════════════
    // AgentActivateSprintHandler
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task AgentActivateSprint_HappyPath_TransicionaAActive()
    {
        await using var db = CreateDb();
        var person = await AddPersonAsync(db);
        var project = await AddProjectAsync(db);

        var sprint = Sprint.Create(project.Id, "Sprint 1", null, null, null, null);
        db.Sprints.Add(sprint);
        await db.SaveChangesAsync();

        var handler = new AgentActivateSprintHandler(CreateSender(db));
        await handler.Handle(
            new AgentActivateSprintCommand(person.Id, sprint.Id),
            CancellationToken.None);

        var updated = await db.Sprints.FindAsync(sprint.Id);
        updated!.Status.ShouldBe(SprintStatus.Active);
    }

    [Fact]
    public async Task AgentActivateSprint_RequestingPersonId_EsElPersonId()
    {
        await using var db = CreateDb();
        var cmd = new AgentActivateSprintCommand(42, 1);
        cmd.RequestingPersonId.ShouldBe(42);
    }

    [Fact]
    public async Task AgentActivateSprint_SprintInexistente_LanzaKeyNotFound()
    {
        await using var db = CreateDb();

        var handler = new AgentActivateSprintHandler(CreateSender(db));
        await Should.ThrowAsync<KeyNotFoundException>(() =>
            handler.Handle(
                new AgentActivateSprintCommand(1, 99999),
                CancellationToken.None));
    }

    // ══════════════════════════════════════════════════════════════════════════
    // AgentCompleteSprintHandler
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task AgentCompleteSprint_SinTareasYSinCarryOver_CompletaElSprint()
    {
        await using var db = CreateDb();
        var person = await AddPersonAsync(db);
        var project = await AddProjectAsync(db);
        var sprint = await AddActiveSprint(db, project.Id);

        var handler = new AgentCompleteSprintHandler(CreateSender(db));
        await handler.Handle(
            new AgentCompleteSprintCommand(person.Id, sprint.Id, null, null),
            CancellationToken.None);

        var updated = await db.Sprints.FindAsync(sprint.Id);
        updated!.Status.ShouldBe(SprintStatus.Completed);
    }

    [Fact]
    public async Task AgentCompleteSprint_CarryOverBacklog_MueveTareaABacklog()
    {
        await using var db = CreateDb();
        var person = await AddPersonAsync(db);
        var project = await AddProjectAsync(db);
        var sprint = await AddActiveSprint(db, project.Id);

        // Tarea pendiente que necesita carry-over
        var wi = WorkItem.Create(project.Id, "Tarea pendiente", null, WorkItemPriority.Medium,
            null, 0, null, false, null, null, sprint.Id);
        wi.TransitionStatus(WorkItemStatus.InProgress);
        db.WorkItems.Add(wi);
        await db.SaveChangesAsync();

        var handler = new AgentCompleteSprintHandler(CreateSender(db));
        await handler.Handle(
            new AgentCompleteSprintCommand(person.Id, sprint.Id, "Backlog", null),
            CancellationToken.None);

        var updatedSprint = await db.Sprints.FindAsync(sprint.Id);
        updatedSprint!.Status.ShouldBe(SprintStatus.Completed);

        var updatedWi = await db.WorkItems.FindAsync(wi.Id);
        updatedWi!.SprintId.ShouldBeNull();
        updatedWi.Status.ShouldBe(WorkItemStatus.Backlog);
    }

    [Fact]
    public async Task AgentCompleteSprint_CarryOverSprintValido_MueveTareaAlSprint()
    {
        await using var db = CreateDb();
        var person = await AddPersonAsync(db);
        var project = await AddProjectAsync(db);
        var activeSprint = await AddActiveSprint(db, project.Id, "Sprint 1");

        // Sprint destino en Planning
        var targetSprint = Sprint.Create(project.Id, "Sprint 2", null, null, null, null);
        db.Sprints.Add(targetSprint);
        await db.SaveChangesAsync();

        var wi = WorkItem.Create(project.Id, "Tarea", null, WorkItemPriority.Medium,
            null, 0, null, false, null, null, activeSprint.Id);
        wi.TransitionStatus(WorkItemStatus.InProgress);
        db.WorkItems.Add(wi);
        await db.SaveChangesAsync();

        var handler = new AgentCompleteSprintHandler(CreateSender(db));
        await handler.Handle(
            new AgentCompleteSprintCommand(person.Id, activeSprint.Id, "Sprint", targetSprint.Id),
            CancellationToken.None);

        var updatedWi = await db.WorkItems.FindAsync(wi.Id);
        updatedWi!.SprintId.ShouldBe(targetSprint.Id);
    }

    [Fact]
    public async Task AgentCompleteSprint_CarryOverInvalido_LanzaInvalidOperation()
    {
        await using var db = CreateDb();
        var person = await AddPersonAsync(db);
        var project = await AddProjectAsync(db);
        var sprint = await AddActiveSprint(db, project.Id);

        // Tarea para forzar el carry-over
        var wi = WorkItem.Create(project.Id, "Tarea", null, WorkItemPriority.Medium,
            null, 0, null, false, null, null, sprint.Id);
        wi.TransitionStatus(WorkItemStatus.InProgress);
        db.WorkItems.Add(wi);
        await db.SaveChangesAsync();

        var handler = new AgentCompleteSprintHandler(CreateSender(db));
        var ex = await Should.ThrowAsync<InvalidOperationException>(() =>
            handler.Handle(
                new AgentCompleteSprintCommand(person.Id, sprint.Id, "InvalidoTotalmenteFalso", null),
                CancellationToken.None));

        ex.Message.ShouldContain("CarryOver");
        ex.Message.ShouldContain("Backlog");
        ex.Message.ShouldContain("Sprint");
    }

    [Fact]
    public async Task AgentCompleteSprint_RequestingPersonId_EsElPersonId()
    {
        var cmd = new AgentCompleteSprintCommand(77, 1, null, null);
        cmd.RequestingPersonId.ShouldBe(77);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // AgentCreateEpicHandler
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task AgentCreateEpic_HappyPath_CreaEpicaYDevuelveId()
    {
        await using var db = CreateDb();
        var person = await AddPersonAsync(db);
        var project = await AddProjectAsync(db);

        var handler = new AgentCreateEpicHandler(CreateSender(db));
        var id = await handler.Handle(
            new AgentCreateEpicCommand(
                person.Id, project.Id,
                "Épica de integración", "Descripción detallada",
                Priority: 2, SortOrder: 1,
                EstimationHours: 40, EstimationPoints: 8),
            CancellationToken.None);

        id.ShouldBeGreaterThan(0);
        var epic = await db.Epics.FindAsync(id);
        epic.ShouldNotBeNull();
        epic.Title.ShouldBe("Épica de integración");
        epic.Description.ShouldBe("Descripción detallada");
        epic.Priority.ShouldBe(2);
    }

    [Fact]
    public async Task AgentCreateEpic_RequestingPersonId_EsElPersonId()
    {
        var cmd = new AgentCreateEpicCommand(55, 1, "Título", null, 0, 0, null, null);
        cmd.RequestingPersonId.ShouldBe(55);
    }

    [Fact]
    public async Task AgentCreateEpic_ProyectoInexistente_LanzaKeyNotFound()
    {
        await using var db = CreateDb();
        var handler = new AgentCreateEpicHandler(CreateSender(db));

        await Should.ThrowAsync<KeyNotFoundException>(() =>
            handler.Handle(
                new AgentCreateEpicCommand(1, 99999, "Épica", null, 0, 0, null, null),
                CancellationToken.None));
    }

    // ══════════════════════════════════════════════════════════════════════════
    // AgentUpdateEpicHandler
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task AgentUpdateEpic_HappyPath_ActualizaLaEpica()
    {
        await using var db = CreateDb();
        var person = await AddPersonAsync(db);
        var project = await AddProjectAsync(db);
        var epic = await AddEpicAsync(db, project.Id, "Título original");

        var handler = new AgentUpdateEpicHandler(CreateSender(db));
        await handler.Handle(
            new AgentUpdateEpicCommand(
                person.Id, epic.Id,
                "Título actualizado", "Nueva descripción",
                Priority: 3, SortOrder: 2,
                EstimationHours: 20, EstimationPoints: 5),
            CancellationToken.None);

        var updated = await db.Epics.FindAsync(epic.Id);
        updated!.Title.ShouldBe("Título actualizado");
        updated.Description.ShouldBe("Nueva descripción");
        updated.Priority.ShouldBe(3);
    }

    [Fact]
    public async Task AgentUpdateEpic_RequestingPersonId_EsElPersonId()
    {
        var cmd = new AgentUpdateEpicCommand(33, 1, "Título", null, 0, 0, null, null);
        cmd.RequestingPersonId.ShouldBe(33);
    }

    [Fact]
    public async Task AgentUpdateEpic_EpicaInexistente_LanzaKeyNotFound()
    {
        await using var db = CreateDb();
        var handler = new AgentUpdateEpicHandler(CreateSender(db));

        await Should.ThrowAsync<KeyNotFoundException>(() =>
            handler.Handle(
                new AgentUpdateEpicCommand(1, 99999, "Título", null, 0, 0, null, null),
                CancellationToken.None));
    }
}
