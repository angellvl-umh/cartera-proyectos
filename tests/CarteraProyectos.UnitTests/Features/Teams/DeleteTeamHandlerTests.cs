using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Features.Teams;
using CarteraProyectos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace CarteraProyectos.UnitTests.Features.Teams;

public class DeleteTeamHandlerTests
{
    private static AppDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static async Task<(AppDbContext db, Person gestor)> DbWithGestor()
    {
        var db = CreateInMemoryContext();
        var gestor = Person.CreateFromClaims("sub-gestor", "Gestor", "gestor@test.com", PersonRole.Gestor);
        db.Persons.Add(gestor);
        await db.SaveChangesAsync();
        return (db, gestor);
    }

    [Fact]
    public async Task Handle_TeamNotFound_ThrowsKeyNotFoundException()
    {
        var (db, gestor) = await DbWithGestor();
        await using var _ = db;
        var handler = new DeleteTeamHandler(db);

        await Should.ThrowAsync<KeyNotFoundException>(
            () => handler.Handle(new DeleteTeamCommand(999, gestor.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_TeamWithActiveProject_ThrowsInvalidOperationException()
    {
        var (db, gestor) = await DbWithGestor();
        await using var _ = db;

        var team = Team.Create("Equipo Activo", null, null);
        db.Teams.Add(team);

        var project = Project.Create("Proyecto Activo", null, "Unidad TIC", ProjectComplexity.Small, null, null, null);
        // Ruta válida: Stopped → PlanningWithClient → PlanningSprint → InSprint
        project.TransitionTo(ProjectStatus.PlanningWithClient);
        project.TransitionTo(ProjectStatus.PlanningSprint);
        project.TransitionTo(ProjectStatus.InSprint);
        db.Projects.Add(project);

        await db.SaveChangesAsync();

        var assignment = ProjectTeamAssignment.Create(project.Id, team.Id, true);
        db.ProjectTeamAssignments.Add(assignment);
        await db.SaveChangesAsync();

        var handler = new DeleteTeamHandler(db);

        await Should.ThrowAsync<InvalidOperationException>(
            () => handler.Handle(new DeleteTeamCommand(team.Id, gestor.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_TeamWithNoActiveProjects_DeletesTeam()
    {
        var dbName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        int teamId;
        int gestorId;

        await using (var seedDb = new AppDbContext(options))
        {
            var gestor = Person.CreateFromClaims("sub-gestor-seed", "Gestor", "gestor-seed@test.com", PersonRole.Gestor);
            seedDb.Persons.Add(gestor);

            var team = Team.Create("Equipo Sin Activos", null, null);
            seedDb.Teams.Add(team);

            var project = Project.Create("Proyecto Cancelado", null, "Unidad TIC", ProjectComplexity.VerySmall, null, null, null);
            // El proyecto nace en Stopped, que es el estado que queremos
            seedDb.Projects.Add(project);

            await seedDb.SaveChangesAsync();

            var assignment = ProjectTeamAssignment.Create(project.Id, team.Id, false);
            seedDb.ProjectTeamAssignments.Add(assignment);
            await seedDb.SaveChangesAsync();

            teamId = team.Id;
            gestorId = gestor.Id;
        }

        await using var db = new AppDbContext(options);
        var handler = new DeleteTeamHandler(db);

        await handler.Handle(new DeleteTeamCommand(teamId, gestorId), CancellationToken.None);

        var deletedTeam = await db.Teams.FindAsync(teamId);
        deletedTeam.ShouldBeNull();
    }

    [Fact]
    public async Task Handle_TeamWithNoProjects_DeletesTeam()
    {
        var (db, gestor) = await DbWithGestor();
        await using var _ = db;

        var team = Team.Create("Equipo Vacío", null, null);
        db.Teams.Add(team);
        await db.SaveChangesAsync();

        var handler = new DeleteTeamHandler(db);

        await handler.Handle(new DeleteTeamCommand(team.Id, gestor.Id), CancellationToken.None);

        var deletedTeam = await db.Teams.FindAsync(team.Id);
        deletedTeam.ShouldBeNull();
    }
}
