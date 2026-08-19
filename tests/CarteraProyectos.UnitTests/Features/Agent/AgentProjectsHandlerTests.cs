using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Features.Agent;
using CarteraProyectos.Core.Features.Projects;
using CarteraProyectos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace CarteraProyectos.UnitTests.Features.Agent;

public class AgentProjectsHandlerTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static ProjectLifecycleService CreateService(AppDbContext db)
        => new ProjectLifecycleService(db);

    private static async Task<Person> AddPersonAsync(AppDbContext db, string email, PersonRole role)
    {
        var person = Person.CreateFromClaims(Guid.NewGuid().ToString(), email.Split('@')[0], email, role);
        db.Persons.Add(person);
        await db.SaveChangesAsync();
        return person;
    }

    [Fact]
    public async Task AgentCreateProject_Gestor_CreaProyecto()
    {
        await using var db = CreateDb();
        var gestor = await AddPersonAsync(db, "gestor@uni.es", PersonRole.Gestor);

        var handler = new AgentCreateProjectHandler(CreateService(db));
        var id = await handler.Handle(
            new AgentCreateProjectCommand(
                gestor.Id, "Nuevo Proyecto", "Descripción del proyecto", "Unidad Solicitante",
                "Medium", 2024, new DateOnly(2024, 1, 1), new DateOnly(2024, 3, 1),
                100, null, null, 3, null, null, null, 50000m, 4),
            CancellationToken.None);

        var created = await db.Projects.FindAsync(id);
        created.ShouldNotBeNull();
        created.Title.ShouldBe("Nuevo Proyecto");
        created.Description.ShouldBe("Descripción del proyecto");
        created.Complexity.ShouldBe(ProjectComplexity.Medium);
        created.BusinessValue.ShouldBe(4);
        created.Status.ShouldBe(ProjectStatus.Stopped);
    }

    [Fact]
    public async Task AgentCreateProject_Desarrollador_LanzaUnauthorized()
    {
        await using var db = CreateDb();
        var dev = await AddPersonAsync(db, "dev@uni.es", PersonRole.Desarrollador);

        var handler = new AgentCreateProjectHandler(CreateService(db));

        await Should.ThrowAsync<UnauthorizedAccessException>(() =>
            handler.Handle(
                new AgentCreateProjectCommand(
                    dev.Id, "Nuevo Proyecto", null, null, "Medium", null, null, null,
                    null, null, null, null, null, null, null, null, null),
                CancellationToken.None));
    }

    [Fact]
    public async Task AgentCreateProject_ComplejidadInvalida_LanzaInvalidOperation()
    {
        await using var db = CreateDb();
        var gestor = await AddPersonAsync(db, "gestor@uni.es", PersonRole.Gestor);

        var handler = new AgentCreateProjectHandler(CreateService(db));

        await Should.ThrowAsync<InvalidOperationException>(() =>
            handler.Handle(
                new AgentCreateProjectCommand(
                    gestor.Id, "Nuevo Proyecto", null, null, "Gigante", null, null, null,
                    null, null, null, null, null, null, null, null, null),
                CancellationToken.None));
    }

    [Fact]
    public async Task AgentUpdateProject_Gestor_ActualizacionParcial()
    {
        await using var db = CreateDb();
        var gestor = await AddPersonAsync(db, "gestor@uni.es", PersonRole.Gestor);

        // Crear un proyecto con valores iniciales
        var project = Project.Create(
            "Proyecto Original", "Descripción original", "Unidad 1",
            ProjectComplexity.Small, 2024, null, null);
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        var handler = new AgentUpdateProjectHandler(db, CreateService(db));
        // Actualizar solo Title y BusinessValue, enviando null para Complexity
        await handler.Handle(
            new AgentUpdateProjectCommand(
                gestor.Id, project.Id,
                "Proyecto Modificado",  // Title
                null,                    // Description
                null,                    // RequestingUnit
                null,                    // Complexity
                null,                    // PortfolioYear
                null,                    // StartDate
                null,                    // EndDate
                null,                    // BeneficiaryCount
                null,                    // PromoterId
                null,                    // OrganicUnitId
                null,                    // GroupPriority
                null,                    // DesiredDeploymentDate
                null,                    // SpecificationsUrl
                null,                    // EpicUrl
                null,                    // EstimatedBudget
                5),                      // BusinessValue
            CancellationToken.None);

        var updated = await db.Projects.FindAsync(project.Id);
        updated!.Title.ShouldBe("Proyecto Modificado");
        updated.Description.ShouldBe("Descripción original"); // Se conserva
        updated.Complexity.ShouldBe(ProjectComplexity.Small); // Se conserva
        updated.BusinessValue.ShouldBe(5);
    }

    [Fact]
    public async Task AgentUpdateProject_NoExiste_LanzaKeyNotFound()
    {
        await using var db = CreateDb();
        var gestor = await AddPersonAsync(db, "gestor@uni.es", PersonRole.Gestor);

        var handler = new AgentUpdateProjectHandler(db, CreateService(db));

        await Should.ThrowAsync<KeyNotFoundException>(() =>
            handler.Handle(
                new AgentUpdateProjectCommand(
                    gestor.Id, 9999, "Nuevo Titulo", null, null, null, null, null, null,
                    null, null, null, null, null, null, null, null, null),
                CancellationToken.None));
    }

    [Fact]
    public async Task AgentUpdateProject_Desarrollador_LanzaUnauthorized()
    {
        await using var db = CreateDb();
        var dev = await AddPersonAsync(db, "dev@uni.es", PersonRole.Desarrollador);

        // Crear un proyecto
        var project = Project.Create(
            "Proyecto", null, null, ProjectComplexity.Small, null, null, null);
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        var handler = new AgentUpdateProjectHandler(db, CreateService(db));

        await Should.ThrowAsync<UnauthorizedAccessException>(() =>
            handler.Handle(
                new AgentUpdateProjectCommand(
                    dev.Id, project.Id,
                    "Nuevo Titulo", null, null, null, null, null, null,
                    null, null, null, null, null, null, null, null, null),
                CancellationToken.None));
    }

    // ══════════════════════════════════════════════════════════════════════════
    // AgentAssignProjectTeamHandler
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task AgentAssignProjectTeam_Gestor_AsignaEquipoAlProyecto()
    {
        await using var db = CreateDb();
        var gestor = await AddPersonAsync(db, "gestor@uni.es", PersonRole.Gestor);

        var project = Project.Create("Proyecto Test", null, null, ProjectComplexity.Small, null, null, null);
        db.Projects.Add(project);
        var team = Team.Create("Equipo Alpha", null, null);
        db.Teams.Add(team);
        await db.SaveChangesAsync();

        var handler = new AgentAssignProjectTeamHandler(CreateService(db));
        await handler.Handle(
            new AgentAssignProjectTeamCommand(gestor.Id, project.Id, team.Id, IsPrimary: true),
            CancellationToken.None);

        var assignment = await db.ProjectTeamAssignments
            .FirstOrDefaultAsync(a => a.ProjectId == project.Id && a.TeamId == team.Id);

        assignment.ShouldNotBeNull();
        assignment.IsPrimary.ShouldBeTrue();
    }

    [Fact]
    public async Task AgentAssignProjectTeam_RequestingPersonId_EsElPersonId()
    {
        var cmd = new AgentAssignProjectTeamCommand(42, 1, 1, true);
        cmd.RequestingPersonId.ShouldBe(42);
    }

    [Fact]
    public async Task AgentAssignProjectTeam_Desarrollador_LanzaUnauthorized()
    {
        await using var db = CreateDb();
        var dev = await AddPersonAsync(db, "dev@uni.es", PersonRole.Desarrollador);

        var project = Project.Create("Proyecto", null, null, ProjectComplexity.Small, null, null, null);
        db.Projects.Add(project);
        var team = Team.Create("Equipo", null, null);
        db.Teams.Add(team);
        await db.SaveChangesAsync();

        var handler = new AgentAssignProjectTeamHandler(CreateService(db));

        var ex = await Should.ThrowAsync<UnauthorizedAccessException>(() =>
            handler.Handle(
                new AgentAssignProjectTeamCommand(dev.Id, project.Id, team.Id, IsPrimary: false),
                CancellationToken.None));

        ex.Message.ShouldContain("Gestor");
    }

    [Fact]
    public async Task AgentAssignProjectTeam_ProyectoInexistente_LanzaKeyNotFound()
    {
        await using var db = CreateDb();
        var gestor = await AddPersonAsync(db, "gestor@uni.es", PersonRole.Gestor);
        var team = Team.Create("Equipo", null, null);
        db.Teams.Add(team);
        await db.SaveChangesAsync();

        var handler = new AgentAssignProjectTeamHandler(CreateService(db));

        await Should.ThrowAsync<KeyNotFoundException>(() =>
            handler.Handle(
                new AgentAssignProjectTeamCommand(gestor.Id, 99999, team.Id, IsPrimary: true),
                CancellationToken.None));
    }

    [Fact]
    public async Task AgentAssignProjectTeam_EquipoInexistente_LanzaKeyNotFound()
    {
        await using var db = CreateDb();
        var gestor = await AddPersonAsync(db, "gestor@uni.es", PersonRole.Gestor);

        var project = Project.Create("Proyecto", null, null, ProjectComplexity.Small, null, null, null);
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        var handler = new AgentAssignProjectTeamHandler(CreateService(db));

        await Should.ThrowAsync<KeyNotFoundException>(() =>
            handler.Handle(
                new AgentAssignProjectTeamCommand(gestor.Id, project.Id, 99999, IsPrimary: true),
                CancellationToken.None));
    }
}
