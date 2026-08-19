using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Features.Agent;
using CarteraProyectos.Core.Features.WorkItems;
using CarteraProyectos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace CarteraProyectos.UnitTests.Features.Agent;

public class AgentUpdateTaskStatusHandlerTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static AgentUpdateTaskStatusHandler CreateHandler(AppDbContext db)
        => new AgentUpdateTaskStatusHandler(new WorkItemLifecycleService(db));

    private static async Task<Person> AddPersonAsync(AppDbContext db, string email, PersonRole role)
    {
        var person = Person.CreateFromClaims(Guid.NewGuid().ToString(), email.Split('@')[0], email, role);
        db.Persons.Add(person);
        await db.SaveChangesAsync();
        return person;
    }

    private static async Task<Project> AddProjectAsync(AppDbContext db, string title)
    {
        var project = Project.Create(title, null, "TIC", ProjectComplexity.Small, null, null, null);
        db.Projects.Add(project);
        await db.SaveChangesAsync();
        return project;
    }

    private static async Task<Team> AddTeamAsync(AppDbContext db, string name)
    {
        var team = Team.Create(name, null, null);
        db.Teams.Add(team);
        await db.SaveChangesAsync();
        return team;
    }

    private static async Task AssignPersonToTeamAsync(AppDbContext db, int personId, int teamId)
    {
        db.PersonTeamMemberships.Add(PersonTeamMembership.Create(personId, teamId));
        await db.SaveChangesAsync();
    }

    private static async Task AssignProjectToTeamAsync(AppDbContext db, int projectId, int teamId)
    {
        db.ProjectTeamAssignments.Add(ProjectTeamAssignment.Create(projectId, teamId, isPrimary: true));
        await db.SaveChangesAsync();
    }

    private static async Task<WorkItem> AddWorkItemAsync(AppDbContext db, int projectId)
    {
        var wi = WorkItem.Create(projectId, "Tarea test", null, WorkItemPriority.Medium, null, 0, null, false, null, null);
        db.WorkItems.Add(wi);
        await db.SaveChangesAsync();
        return wi;
    }

    [Fact]
    public async Task AgentUpdateTaskStatus_MiembroEquipo_CambiaEstadoYCreaHistoria()
    {
        await using var db = CreateDb();
        var dev = await AddPersonAsync(db, "dev@uni.es", PersonRole.Desarrollador);
        var team = await AddTeamAsync(db, "Equipo Alpha");
        await AssignPersonToTeamAsync(db, dev.Id, team.Id);
        var project = await AddProjectAsync(db, "Proyecto 1");
        await AssignProjectToTeamAsync(db, project.Id, team.Id);
        var wi = await AddWorkItemAsync(db, project.Id);

        var handler = CreateHandler(db);
        await handler.Handle(
            new AgentUpdateTaskStatusCommand(dev.Id, wi.Id, "InProgress"),
            CancellationToken.None);

        var updated = await db.WorkItems.FindAsync(wi.Id);
        updated!.Status.ShouldBe(WorkItemStatus.InProgress);

        var history = await db.WorkItemStatusHistories
            .Where(h => h.WorkItemId == wi.Id)
            .ToListAsync();
        history.Count.ShouldBe(1);
        history[0].FromStatus.ShouldBe(WorkItemStatus.Backlog);
        history[0].ToStatus.ShouldBe(WorkItemStatus.InProgress);
    }

    [Fact]
    public async Task AgentUpdateTaskStatus_PersonaAjena_LanzaUnauthorized()
    {
        await using var db = CreateDb();
        var dev1 = await AddPersonAsync(db, "dev1@uni.es", PersonRole.Desarrollador);
        var dev2 = await AddPersonAsync(db, "dev2@uni.es", PersonRole.Desarrollador);
        var team1 = await AddTeamAsync(db, "Equipo 1");
        var team2 = await AddTeamAsync(db, "Equipo 2");
        await AssignPersonToTeamAsync(db, dev1.Id, team1.Id);
        await AssignPersonToTeamAsync(db, dev2.Id, team2.Id);

        var project = await AddProjectAsync(db, "Proyecto 1");
        await AssignProjectToTeamAsync(db, project.Id, team1.Id); // Solo asignado a Equipo 1
        var wi = await AddWorkItemAsync(db, project.Id);

        var handler = CreateHandler(db);

        // dev2 está en Equipo 2, que no tiene acceso al proyecto
        await Should.ThrowAsync<UnauthorizedAccessException>(() =>
            handler.Handle(
                new AgentUpdateTaskStatusCommand(dev2.Id, wi.Id, "InProgress"),
                CancellationToken.None));
    }

    [Fact]
    public async Task AgentUpdateTaskStatus_EstadoInvalido_LanzaInvalidOperation()
    {
        await using var db = CreateDb();
        var dev = await AddPersonAsync(db, "dev@uni.es", PersonRole.Desarrollador);
        var team = await AddTeamAsync(db, "Equipo Alpha");
        await AssignPersonToTeamAsync(db, dev.Id, team.Id);
        var project = await AddProjectAsync(db, "Proyecto 1");
        await AssignProjectToTeamAsync(db, project.Id, team.Id);
        var wi = await AddWorkItemAsync(db, project.Id);

        var handler = CreateHandler(db);

        await Should.ThrowAsync<InvalidOperationException>(() =>
            handler.Handle(
                new AgentUpdateTaskStatusCommand(dev.Id, wi.Id, "InvalidStatus"),
                CancellationToken.None));
    }

    [Fact]
    public async Task AgentUpdateTaskStatus_DesdeBacklogToInProgress_CreaHistoria()
    {
        await using var db = CreateDb();
        var gestor = await AddPersonAsync(db, "gestor@uni.es", PersonRole.Gestor);
        var project = await AddProjectAsync(db, "Proyecto 1");
        var wi = await AddWorkItemAsync(db, project.Id);

        // Gestor puede cambiar estado de tareas de sus proyectos
        var handler = CreateHandler(db);

        await handler.Handle(
            new AgentUpdateTaskStatusCommand(gestor.Id, wi.Id, "ToDo"),
            CancellationToken.None);

        await handler.Handle(
            new AgentUpdateTaskStatusCommand(gestor.Id, wi.Id, "InProgress"),
            CancellationToken.None);

        var history = await db.WorkItemStatusHistories
            .Where(h => h.WorkItemId == wi.Id)
            .OrderBy(h => h.ChangedAt)
            .ToListAsync();

        history.Count.ShouldBe(2);
        history[0].FromStatus.ShouldBe(WorkItemStatus.Backlog);
        history[0].ToStatus.ShouldBe(WorkItemStatus.ToDo);
        history[1].FromStatus.ShouldBe(WorkItemStatus.ToDo);
        history[1].ToStatus.ShouldBe(WorkItemStatus.InProgress);
    }
}
