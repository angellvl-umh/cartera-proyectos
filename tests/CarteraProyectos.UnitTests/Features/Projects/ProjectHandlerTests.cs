using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Features.Projects;
using CarteraProyectos.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace CarteraProyectos.UnitTests.Features.Projects;

public class ProjectHandlerTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static async Task<(AppDbContext db, Person gestor)> DbWithGestor()
    {
        var db = CreateDb();
        var gestor = Person.CreateFromClaims("sub-gestor", "Gestor", "gestor@test.com", PersonRole.Gestor);
        db.Persons.Add(gestor);
        await db.SaveChangesAsync();
        return (db, gestor);
    }

    // --- CreateProject ---

    [Fact]
    public async Task CreateProject_ValidCommand_CreatesProjectWithStoppedStatus()
    {
        var (db, gestor) = await DbWithGestor();
        var service = new ProjectLifecycleService(db);
        var handler = new CreateProjectHandler(service);

        var id = await handler.Handle(
            new CreateProjectCommand("Portal Alumno", null, "RRHH", ProjectComplexity.Medium, 2026, null, null,
                RequestingPersonId: gestor.Id),
            CancellationToken.None);

        id.ShouldBeGreaterThan(0);
        var project = await db.Projects.FindAsync(id);
        project.ShouldNotBeNull();
        project.Title.ShouldBe("Portal Alumno");
        project.Status.ShouldBe(ProjectStatus.Stopped);
        project.RequestingUnit.ShouldBe("RRHH");
        project.Complexity.ShouldBe(ProjectComplexity.Medium);
    }

    [Fact]
    public async Task CreateProject_WithNewFields_StoresAllFields()
    {
        var (db, gestor) = await DbWithGestor();
        var service = new ProjectLifecycleService(db);
        var handler = new CreateProjectHandler(service);

        var id = await handler.Handle(
            new CreateProjectCommand(
                "Portal Alumno", null, null, ProjectComplexity.Small, 2026, null, null,
                PreviousReferenceId: 42,
                BeneficiaryCount: 500,
                GroupPriority: 3,
                SpecificationsUrl: "https://drive.google.com/doc",
                EpicUrl: "https://jira.umh.es/epic/1",
                RequestingPersonId: gestor.Id),
            CancellationToken.None);

        var project = await db.Projects.FindAsync(id);
        project.ShouldNotBeNull();
        project.PreviousReferenceId.ShouldBe(42);
        project.BeneficiaryCount.ShouldBe(500);
        project.GroupPriority.ShouldBe(3);
        project.SpecificationsUrl.ShouldBe("https://drive.google.com/doc");
        project.EpicUrl.ShouldBe("https://jira.umh.es/epic/1");
    }

    // --- CreateProject con TeamIds ---

    [Fact]
    public async Task CreateProject_ConTeamIds_CreaAsignacionesDeEquipo()
    {
        var (db, gestor) = await DbWithGestor();
        var team1 = Team.Create("Equipo A", null, null);
        var team2 = Team.Create("Equipo B", null, null);
        db.Teams.AddRange(team1, team2);
        await db.SaveChangesAsync();

        var service = new ProjectLifecycleService(db);
        var handler = new CreateProjectHandler(service);
        var id = await handler.Handle(
            new CreateProjectCommand("Proyecto Test", null, null, ProjectComplexity.Small, 2026, null, null,
                RequestingPersonId: gestor.Id,
                TeamIds: [team1.Id, team2.Id],
                PrimaryTeamId: team1.Id),
            CancellationToken.None);

        var assignments = await db.ProjectTeamAssignments
            .Where(a => a.ProjectId == id)
            .ToListAsync();

        assignments.Count.ShouldBe(2);
        assignments.Single(a => a.TeamId == team1.Id).IsPrimary.ShouldBeTrue();
        assignments.Single(a => a.TeamId == team2.Id).IsPrimary.ShouldBeFalse();
    }

    [Fact]
    public async Task CreateProject_SinTeamIds_NoCreaAsignaciones()
    {
        var (db, gestor) = await DbWithGestor();
        var service = new ProjectLifecycleService(db);
        var handler = new CreateProjectHandler(service);

        var id = await handler.Handle(
            new CreateProjectCommand("Proyecto Sin Equipos", null, null, ProjectComplexity.Small, 2026, null, null,
                RequestingPersonId: gestor.Id),
            CancellationToken.None);

        var assignments = await db.ProjectTeamAssignments
            .Where(a => a.ProjectId == id)
            .ToListAsync();

        assignments.ShouldBeEmpty();
    }

    [Fact]
    public async Task CreateProject_PrimaryTeamIdFueraDeTeamIds_FallaValidacion()
    {
        var validator = new CreateProjectValidator();

        var cmd = new CreateProjectCommand(
            "Proyecto", null, null, ProjectComplexity.Small, null, null, null,
            TeamIds: [1, 2],
            PrimaryTeamId: 99); // 99 no está en [1, 2]

        var result = await validator.ValidateAsync(cmd);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "PrimaryTeamId");
    }

    [Fact]
    public async Task UpdateProject_PrimaryTeamIdFueraDeTeamIds_FallaValidacion()
    {
        var validator = new UpdateProjectValidator();

        var cmd = new UpdateProjectCommand(
            1, "Proyecto", null, null, ProjectComplexity.Small, null, null, null,
            TeamIds: [1, 2],
            PrimaryTeamId: 99); // 99 no está en [1, 2]

        var result = await validator.ValidateAsync(cmd);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "PrimaryTeamId");
    }

    // --- UpdateProject con TeamIds ---

    [Fact]
    public async Task UpdateProject_ConTeamIds_ReemplazaAsignacionesExistentes()
    {
        var (db, gestor) = await DbWithGestor();
        var team1 = Team.Create("Equipo 1", null, null);
        var team2 = Team.Create("Equipo 2", null, null);
        var team3 = Team.Create("Equipo 3", null, null);
        db.Teams.AddRange(team1, team2, team3);

        var project = Project.Create("Proyecto", null, null, ProjectComplexity.Small, 2026, null, null);
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        // Asignar equipos 1 y 2 inicialmente
        db.ProjectTeamAssignments.AddRange(
            ProjectTeamAssignment.Create(project.Id, team1.Id, true),
            ProjectTeamAssignment.Create(project.Id, team2.Id, false));
        await db.SaveChangesAsync();

        var service = new ProjectLifecycleService(db);
        var handler = new UpdateProjectHandler(service);
        await handler.Handle(
            new UpdateProjectCommand(project.Id, "Proyecto", null, null, ProjectComplexity.Small, 2026, null, null,
                TeamIds: [team2.Id, team3.Id],
                PrimaryTeamId: team2.Id),
            CancellationToken.None);

        var assignments = await db.ProjectTeamAssignments
            .Where(a => a.ProjectId == project.Id)
            .ToListAsync();

        assignments.Count.ShouldBe(2);
        assignments.ShouldContain(a => a.TeamId == team2.Id);
        assignments.ShouldContain(a => a.TeamId == team3.Id);
        assignments.ShouldNotContain(a => a.TeamId == team1.Id);
        assignments.Single(a => a.TeamId == team2.Id).IsPrimary.ShouldBeTrue();
    }

    [Fact]
    public async Task UpdateProject_SinTeamIds_NoModificaAsignacionesExistentes()
    {
        var (db, gestor) = await DbWithGestor();
        var team1 = Team.Create("Equipo 1", null, null);
        db.Teams.Add(team1);

        var project = Project.Create("Proyecto", null, null, ProjectComplexity.Small, 2026, null, null);
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        db.ProjectTeamAssignments.Add(
            ProjectTeamAssignment.Create(project.Id, team1.Id, true));
        await db.SaveChangesAsync();

        var service = new ProjectLifecycleService(db);
        var handler = new UpdateProjectHandler(service);
        // TeamIds es null → no tocar equipos
        await handler.Handle(
            new UpdateProjectCommand(project.Id, "Título Nuevo", null, null, ProjectComplexity.Small, 2026, null, null),
            CancellationToken.None);

        var assignments = await db.ProjectTeamAssignments
            .Where(a => a.ProjectId == project.Id)
            .ToListAsync();

        assignments.Count.ShouldBe(1);
        assignments[0].TeamId.ShouldBe(team1.Id);
    }

    // --- UpdateProject ---

    [Fact]
    public async Task UpdateProject_ValidCommand_UpdatesFields()
    {
        await using var db = CreateDb();
        var project = Project.Create("Original", null, "TIC", ProjectComplexity.VerySmall, null, null, null);
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        var service = new ProjectLifecycleService(db);
        var handler = new UpdateProjectHandler(service);
        await handler.Handle(
            new UpdateProjectCommand(project.Id, "Actualizado", "Nueva desc", "ADMIN", ProjectComplexity.Large, 2027, null, null),
            CancellationToken.None);

        var updated = await db.Projects.FindAsync(project.Id);
        updated!.Title.ShouldBe("Actualizado");
        updated.Description.ShouldBe("Nueva desc");
        updated.RequestingUnit.ShouldBe("ADMIN");
        updated.Complexity.ShouldBe(ProjectComplexity.Large);
    }

    [Fact]
    public async Task UpdateProject_NotFound_ThrowsKeyNotFoundException()
    {
        await using var db = CreateDb();
        var service = new ProjectLifecycleService(db);
        var handler = new UpdateProjectHandler(service);

        await Should.ThrowAsync<KeyNotFoundException>(
            () => handler.Handle(
                new UpdateProjectCommand(999, "X", null, "TIC", ProjectComplexity.VerySmall, null, null, null),
                CancellationToken.None));
    }

    // --- DeleteProject ---

    [Fact]
    public async Task DeleteProject_StoppedProject_RemovesIt()
    {
        await using var db = CreateDb();
        var project = Project.Create("A borrar", null, "TIC", ProjectComplexity.VerySmall, null, null, null);
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        var handler = new DeleteProjectHandler(db);
        await handler.Handle(new DeleteProjectCommand(project.Id), CancellationToken.None);

        (await db.Projects.FindAsync(project.Id)).ShouldBeNull();
    }

    [Fact]
    public async Task DeleteProject_NotFound_ThrowsKeyNotFoundException()
    {
        await using var db = CreateDb();
        var handler = new DeleteProjectHandler(db);

        await Should.ThrowAsync<KeyNotFoundException>(
            () => handler.Handle(new DeleteProjectCommand(999), CancellationToken.None));
    }

    [Fact]
    public async Task DeleteProject_ActiveProject_ThrowsInvalidOperationException()
    {
        await using var db = CreateDb();
        var project = Project.Create("Activo", null, "TIC", ProjectComplexity.VerySmall, null, null, null);
        db.Projects.Add(project);
        await db.SaveChangesAsync();
        // Ruta válida: Stopped → PlanningWithClient → PlanningSprint → InSprint
        project.TransitionTo(ProjectStatus.PlanningWithClient);
        project.TransitionTo(ProjectStatus.PlanningSprint);
        project.TransitionTo(ProjectStatus.InSprint);
        await db.SaveChangesAsync();

        var handler = new DeleteProjectHandler(db);
        await Should.ThrowAsync<InvalidOperationException>(
            () => handler.Handle(new DeleteProjectCommand(project.Id), CancellationToken.None));
    }

    // --- GetProjects ---

    [Fact]
    public async Task GetProjects_ReturnsPaged()
    {
        await using var db = CreateDb();
        db.Projects.AddRange(
            Project.Create("P1", null, "TIC", ProjectComplexity.VerySmall, null, null, null),
            Project.Create("P2", null, "TIC", ProjectComplexity.VerySmall, null, null, null),
            Project.Create("P3", null, "TIC", ProjectComplexity.VerySmall, null, null, null));
        await db.SaveChangesAsync();

        var handler = new GetProjectsHandler(db);
        var result = await handler.Handle(new GetProjectsQuery(Page: 1, PageSize: 2), CancellationToken.None);

        result.Total.ShouldBe(3);
        result.Items.Count.ShouldBe(2);
        result.Page.ShouldBe(1);
    }

    // --- ProjectStatusHistory ---

    [Fact]
    public async Task CreateProject_RecordsInitialStatusHistory()
    {
        var (db, gestor) = await DbWithGestor();

        var service = new ProjectLifecycleService(db);
        var handler = new CreateProjectHandler(service);
        var id = await handler.Handle(
            new CreateProjectCommand("Proyecto Test", null, "TIC", ProjectComplexity.Small, 2026, null, null,
                RequestingPersonId: gestor.Id),
            CancellationToken.None);

        var history = await db.ProjectStatusHistories.Where(h => h.ProjectId == id).ToListAsync();
        history.Count.ShouldBe(1);
        history[0].FromStatus.ShouldBeNull();
        history[0].ToStatus.ShouldBe(ProjectStatus.Stopped);
        history[0].ChangedById.ShouldBe(gestor.Id);
    }
}
