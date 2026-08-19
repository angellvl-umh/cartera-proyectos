using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Features.Agent;
using CarteraProyectos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace CarteraProyectos.UnitTests.Features.Agent;

public class AgentGetProjectsHandlerTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

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

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Gestor_SinEquipos_VeTodosLosProyectos()
    {
        await using var db = CreateDb();
        var gestor = await AddPersonAsync(db, "gestor@uni.es", PersonRole.Gestor);
        await AddProjectAsync(db, "Proyecto A");
        await AddProjectAsync(db, "Proyecto B");
        await AddProjectAsync(db, "Proyecto C");

        var handler = new AgentGetProjectsHandler(new ProjectsReadService(db));
        var result = await handler.Handle(new AgentGetProjectsQuery(gestor.Id), CancellationToken.None);

        result.Count.ShouldBe(3);
        result.ShouldContain(p => p.Title == "Proyecto A");
        result.ShouldContain(p => p.Title == "Proyecto B");
        result.ShouldContain(p => p.Title == "Proyecto C");
    }

    [Fact]
    public async Task Gestor_SinProyectosEnSistema_DevuelveListaVacia()
    {
        await using var db = CreateDb();
        var gestor = await AddPersonAsync(db, "gestor@uni.es", PersonRole.Gestor);

        var handler = new AgentGetProjectsHandler(new ProjectsReadService(db));
        var result = await handler.Handle(new AgentGetProjectsQuery(gestor.Id), CancellationToken.None);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task JefeEquipo_SoloVeProyectosDeSusEquipos()
    {
        await using var db = CreateDb();
        var jefe = await AddPersonAsync(db, "jefe@uni.es", PersonRole.JefeEquipo);
        var team = await AddTeamAsync(db, "Equipo Alpha");
        await AssignPersonToTeamAsync(db, jefe.Id, team.Id);

        var suProyecto  = await AddProjectAsync(db, "Proyecto del equipo");
        var otroProyecto = await AddProjectAsync(db, "Proyecto ajeno");
        await AssignProjectToTeamAsync(db, suProyecto.Id, team.Id);

        var handler = new AgentGetProjectsHandler(new ProjectsReadService(db));
        var result = await handler.Handle(new AgentGetProjectsQuery(jefe.Id), CancellationToken.None);

        result.Count.ShouldBe(1);
        result[0].Title.ShouldBe("Proyecto del equipo");
        result[0].PrimaryTeamName.ShouldBe("Equipo Alpha");
    }

    [Fact]
    public async Task Desarrollador_SoloVeProyectosDeSusEquipos()
    {
        await using var db = CreateDb();
        var dev = await AddPersonAsync(db, "dev@uni.es", PersonRole.Desarrollador);
        var team = await AddTeamAsync(db, "Equipo Beta");
        await AssignPersonToTeamAsync(db, dev.Id, team.Id);

        var suProyecto   = await AddProjectAsync(db, "Proyecto visible");
        var otroProyecto = await AddProjectAsync(db, "Proyecto no visible");
        await AssignProjectToTeamAsync(db, suProyecto.Id, team.Id);

        var handler = new AgentGetProjectsHandler(new ProjectsReadService(db));
        var result = await handler.Handle(new AgentGetProjectsQuery(dev.Id), CancellationToken.None);

        result.Count.ShouldBe(1);
        result[0].Title.ShouldBe("Proyecto visible");
    }

    [Fact]
    public async Task Desarrollador_SinEquipos_DevuelveListaVacia()
    {
        await using var db = CreateDb();
        var dev = await AddPersonAsync(db, "dev@uni.es", PersonRole.Desarrollador);
        await AddProjectAsync(db, "Proyecto cualquiera");

        var handler = new AgentGetProjectsHandler(new ProjectsReadService(db));
        var result = await handler.Handle(new AgentGetProjectsQuery(dev.Id), CancellationToken.None);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task PersonaInexistente_LanzaKeyNotFoundException()
    {
        await using var db = CreateDb();
        var handler = new AgentGetProjectsHandler(new ProjectsReadService(db));

        await Should.ThrowAsync<KeyNotFoundException>(() =>
            handler.Handle(new AgentGetProjectsQuery(9999), CancellationToken.None));
    }

    [Fact]
    public async Task Gestor_VeProyectoDeEquipoAjenoTambien()
    {
        await using var db = CreateDb();
        var gestor = await AddPersonAsync(db, "gestor@uni.es", PersonRole.Gestor);
        var dev    = await AddPersonAsync(db, "dev@uni.es", PersonRole.Desarrollador);
        var team   = await AddTeamAsync(db, "Equipo Gamma");
        await AssignPersonToTeamAsync(db, dev.Id, team.Id);

        var proyecto = await AddProjectAsync(db, "Proyecto solo del dev");
        await AssignProjectToTeamAsync(db, proyecto.Id, team.Id);

        var handler = new AgentGetProjectsHandler(new ProjectsReadService(db));
        var result = await handler.Handle(new AgentGetProjectsQuery(gestor.Id), CancellationToken.None);

        // Gestor lo ve aunque no esté en el equipo
        result.Count.ShouldBe(1);
        result[0].Title.ShouldBe("Proyecto solo del dev");
    }
}
