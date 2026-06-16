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

    [Fact]
    public async Task Handle_TeamNotFound_ThrowsKeyNotFoundException()
    {
        await using var db = CreateInMemoryContext();
        var handler = new DeleteTeamHandler(db);

        await Should.ThrowAsync<KeyNotFoundException>(
            () => handler.Handle(new DeleteTeamCommand(999), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_TeamWithActiveProject_ThrowsInvalidOperationException()
    {
        await using var db = CreateInMemoryContext();

        var team = Team.Create("Equipo Activo", null, null);
        db.Teams.Add(team);

        var project = Project.Create("Proyecto Activo", null, "Unidad TIC", ProjectComplexity.Small, null, null, null);
        // Project starts as Proposed; transition to Approved (active)
        project.TransitionTo(ProjectStatus.InSprint);
        db.Projects.Add(project);

        await db.SaveChangesAsync();

        var assignment = ProjectTeamAssignment.Create(project.Id, team.Id, true);
        db.ProjectTeamAssignments.Add(assignment);
        await db.SaveChangesAsync();

        var handler = new DeleteTeamHandler(db);

        await Should.ThrowAsync<InvalidOperationException>(
            () => handler.Handle(new DeleteTeamCommand(team.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_TeamWithNoActiveProjects_DeletesTeam()
    {
        // Use a shared database name so we can open two context instances
        var dbName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        int teamId;
        // Seed with a cancelled project and assignment in one context scope
        await using (var seedDb = new AppDbContext(options))
        {
            var team = Team.Create("Equipo Sin Activos", null, null);
            seedDb.Teams.Add(team);

            var project = Project.Create("Proyecto Cancelado", null, "Unidad TIC", ProjectComplexity.VerySmall, null, null, null);
            project.TransitionTo(ProjectStatus.Stopped);
            seedDb.Projects.Add(project);

            await seedDb.SaveChangesAsync();

            var assignment = ProjectTeamAssignment.Create(project.Id, team.Id, false);
            seedDb.ProjectTeamAssignments.Add(assignment);
            await seedDb.SaveChangesAsync();

            teamId = team.Id;
        }

        // Run the handler in a fresh context (no tracked entities, so no cascade conflict)
        await using var db = new AppDbContext(options);
        var handler = new DeleteTeamHandler(db);

        await handler.Handle(new DeleteTeamCommand(teamId), CancellationToken.None);

        var deletedTeam = await db.Teams.FindAsync(teamId);
        deletedTeam.ShouldBeNull();
    }

    [Fact]
    public async Task Handle_TeamWithNoProjects_DeletesTeam()
    {
        await using var db = CreateInMemoryContext();

        var team = Team.Create("Equipo Vacío", null, null);
        db.Teams.Add(team);
        await db.SaveChangesAsync();

        var handler = new DeleteTeamHandler(db);

        await handler.Handle(new DeleteTeamCommand(team.Id), CancellationToken.None);

        var deletedTeam = await db.Teams.FindAsync(team.Id);
        deletedTeam.ShouldBeNull();
    }
}
