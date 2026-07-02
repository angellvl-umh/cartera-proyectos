using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Features.Projects.WeeklyUpdates;
using CarteraProyectos.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace CarteraProyectos.UnitTests.Features.Projects.WeeklyUpdates;

public class ProjectWeeklyUpdatesHandlerTests
{
    private static AppDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static async Task<(AppDbContext db, Project project)> DbWithProject()
    {
        var db = CreateDb();
        var project = Project.Create("Proyecto Test", null, null, ProjectComplexity.Small, null, null, null);
        db.Projects.Add(project);
        await db.SaveChangesAsync();
        return (db, project);
    }

    private static async Task<Person> AddPersonAsync(AppDbContext db, PersonRole role)
    {
        var person = Person.CreateFromClaims(Guid.NewGuid().ToString(), "user", "user@uni.es", role);
        db.Persons.Add(person);
        await db.SaveChangesAsync();
        return person;
    }

    // --- UpsertProjectWeeklyUpdate ---

    [Fact]
    public async Task Upsert_Gestor_CreatesNewUpdate()
    {
        var (db, project) = await DbWithProject();
        var gestor = await AddPersonAsync(db, PersonRole.Gestor);

        var id = await new UpsertProjectWeeklyUpdateHandler(db).Handle(
            new UpsertProjectWeeklyUpdateCommand(project.Id, gestor.Id, "Semana productiva", ProjectHealthStatus.OnTrack),
            CancellationToken.None);

        id.ShouldBeGreaterThan(0);
        var update = await db.ProjectWeeklyUpdates.FindAsync(id);
        update.ShouldNotBeNull();
        update.Summary.ShouldBe("Semana productiva");
        update.HealthStatus.ShouldBe(ProjectHealthStatus.OnTrack);
    }

    [Fact]
    public async Task Upsert_MismaSemanaMismoAutor_ActualizaNoDuplica()
    {
        var (db, project) = await DbWithProject();
        var gestor = await AddPersonAsync(db, PersonRole.Gestor);

        var id1 = await new UpsertProjectWeeklyUpdateHandler(db).Handle(
            new UpsertProjectWeeklyUpdateCommand(project.Id, gestor.Id, "Primera actualización", ProjectHealthStatus.OnTrack),
            CancellationToken.None);

        var id2 = await new UpsertProjectWeeklyUpdateHandler(db).Handle(
            new UpsertProjectWeeklyUpdateCommand(project.Id, gestor.Id, "Segunda actualización", ProjectHealthStatus.AtRisk),
            CancellationToken.None);

        id1.ShouldBe(id2);
        var updates = await db.ProjectWeeklyUpdates
            .Where(u => u.ProjectId == project.Id && u.AuthorId == gestor.Id)
            .ToListAsync();
        updates.Count.ShouldBe(1);
        updates[0].Summary.ShouldBe("Segunda actualización");
        updates[0].HealthStatus.ShouldBe(ProjectHealthStatus.AtRisk);
    }

    [Fact]
    public async Task Upsert_DesarrolladorSinEquipoAsignado_ThrowsUnauthorizedAccessException()
    {
        var (db, project) = await DbWithProject();
        var dev = await AddPersonAsync(db, PersonRole.Desarrollador);

        await Should.ThrowAsync<UnauthorizedAccessException>(() =>
            new UpsertProjectWeeklyUpdateHandler(db).Handle(
                new UpsertProjectWeeklyUpdateCommand(project.Id, dev.Id, "Texto", ProjectHealthStatus.OnTrack),
                CancellationToken.None));
    }

    [Fact]
    public async Task Upsert_DesarrolladorEnEquipoAsignado_CreatesUpdate()
    {
        var (db, project) = await DbWithProject();
        var dev = await AddPersonAsync(db, PersonRole.Desarrollador);

        var team = Team.Create("Equipo Test", null, null);
        db.Teams.Add(team);
        await db.SaveChangesAsync();
        db.ProjectTeamAssignments.Add(ProjectTeamAssignment.Create(project.Id, team.Id, true));
        await db.SaveChangesAsync();
        db.PersonTeamMemberships.Add(PersonTeamMembership.Create(dev.Id, team.Id));
        await db.SaveChangesAsync();

        var id = await new UpsertProjectWeeklyUpdateHandler(db).Handle(
            new UpsertProjectWeeklyUpdateCommand(project.Id, dev.Id, "Texto", ProjectHealthStatus.OnTrack),
            CancellationToken.None);

        id.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task Upsert_MiembroEquipoAsignado_CreatesUpdate()
    {
        var (db, project) = await DbWithProject();
        var jefe = await AddPersonAsync(db, PersonRole.JefeEquipo);

        var team = Team.Create("Equipo Asignado", null, null); // miembro, no líder
        db.Teams.Add(team);
        await db.SaveChangesAsync();
        db.PersonTeamMemberships.Add(PersonTeamMembership.Create(jefe.Id, team.Id));
        db.ProjectTeamAssignments.Add(ProjectTeamAssignment.Create(project.Id, team.Id, true));
        await db.SaveChangesAsync();

        var id = await new UpsertProjectWeeklyUpdateHandler(db).Handle(
            new UpsertProjectWeeklyUpdateCommand(project.Id, jefe.Id, "Nota del miembro", ProjectHealthStatus.OnTrack),
            CancellationToken.None);

        id.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task Upsert_PersonaAjenaAlProyecto_ThrowsUnauthorizedAccessException()
    {
        var (db, project) = await DbWithProject();
        var jefe = await AddPersonAsync(db, PersonRole.JefeEquipo);

        // El jefe pertenece a un equipo que NO está asignado al proyecto
        var team = Team.Create("Equipo No Asignado", null, null);
        db.Teams.Add(team);
        await db.SaveChangesAsync();
        db.PersonTeamMemberships.Add(PersonTeamMembership.Create(jefe.Id, team.Id));
        await db.SaveChangesAsync();

        await Should.ThrowAsync<UnauthorizedAccessException>(() =>
            new UpsertProjectWeeklyUpdateHandler(db).Handle(
                new UpsertProjectWeeklyUpdateCommand(project.Id, jefe.Id, "Texto", ProjectHealthStatus.OnTrack),
                CancellationToken.None));
    }

    [Fact]
    public async Task Upsert_ProyectoNoExiste_ThrowsKeyNotFoundException()
    {
        await using var db = CreateDb();
        var gestor = await AddPersonAsync(db, PersonRole.Gestor);

        await Should.ThrowAsync<KeyNotFoundException>(() =>
            new UpsertProjectWeeklyUpdateHandler(db).Handle(
                new UpsertProjectWeeklyUpdateCommand(999, gestor.Id, "Texto", ProjectHealthStatus.OnTrack),
                CancellationToken.None));
    }

    [Fact]
    public void Upsert_SummaryVacio_FailsValidation()
    {
        var validator = new UpsertProjectWeeklyUpdateValidator();
        var command = new UpsertProjectWeeklyUpdateCommand(1, 1, "", ProjectHealthStatus.OnTrack);
        var result = validator.Validate(command);
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Upsert_SummaryMayorA1000Caracteres_FailsValidation()
    {
        var validator = new UpsertProjectWeeklyUpdateValidator();
        var command = new UpsertProjectWeeklyUpdateCommand(1, 1, new string('a', 1001), ProjectHealthStatus.OnTrack);
        var result = validator.Validate(command);
        result.IsValid.ShouldBeFalse();
    }

    // --- GetProjectWeeklyUpdates ---

    [Fact]
    public async Task GetProjectWeeklyUpdates_ExistingProject_ReturnsUpdatesOrderedByWeekOfDescending()
    {
        var (db, project) = await DbWithProject();
        var author = await AddPersonAsync(db, PersonRole.Gestor);

        var update1 = ProjectWeeklyUpdate.Create(project.Id, author.Id, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7)), "Primera semana", ProjectHealthStatus.OnTrack);
        var update2 = ProjectWeeklyUpdate.Create(project.Id, author.Id, DateOnly.FromDateTime(DateTime.UtcNow), "Segunda semana", ProjectHealthStatus.AtRisk);
        db.ProjectWeeklyUpdates.AddRange(update1, update2);
        await db.SaveChangesAsync();

        var result = await new GetProjectWeeklyUpdatesHandler(db).Handle(
            new GetProjectWeeklyUpdatesQuery(project.Id), CancellationToken.None);

        result.Count.ShouldBe(2);
        result[0].Summary.ShouldBe("Segunda semana");
        result[1].Summary.ShouldBe("Primera semana");
    }

    [Fact]
    public async Task GetProjectWeeklyUpdates_ProjectNotFound_ThrowsKeyNotFoundException()
    {
        await using var db = CreateDb();

        await Should.ThrowAsync<KeyNotFoundException>(() =>
            new GetProjectWeeklyUpdatesHandler(db).Handle(
                new GetProjectWeeklyUpdatesQuery(999), CancellationToken.None));
    }

    [Fact]
    public async Task GetProjectWeeklyUpdates_IncludesAuthorName()
    {
        var (db, project) = await DbWithProject();
        var author = await AddPersonAsync(db, PersonRole.Gestor);

        var update = ProjectWeeklyUpdate.Create(project.Id, author.Id, DateOnly.FromDateTime(DateTime.UtcNow), "Test", ProjectHealthStatus.OnTrack);
        db.ProjectWeeklyUpdates.Add(update);
        await db.SaveChangesAsync();

        var result = await new GetProjectWeeklyUpdatesHandler(db).Handle(
            new GetProjectWeeklyUpdatesQuery(project.Id), CancellationToken.None);

        result.Count.ShouldBe(1);
        result[0].AuthorName.ShouldBe("user");
        result[0].HealthStatus.ShouldBe("OnTrack");
    }
}
