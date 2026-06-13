using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Features.Projects;
using CarteraProyectos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace CarteraProyectos.UnitTests.Features.Projects;

public class TransitionProjectStatusHandlerTests
{
    private static AppDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static async Task<(AppDbContext db, Project project, Person gestor)> SetupProjectWithGestor()
    {
        var db = CreateInMemoryContext();

        var gestor = Person.CreateFromClaims("sub-gestor", "Gestor User", "gestor@test.com", PersonRole.Gestor);
        db.Persons.Add(gestor);

        var project = Project.Create("Proyecto Test", null, "Unidad TIC", ProjectComplexity.Medium, null, null, null);
        db.Projects.Add(project);

        await db.SaveChangesAsync();
        return (db, project, gestor);
    }

    [Fact]
    public async Task Handle_ValidTransition_ProposedToApproved_ByGestor_Succeeds()
    {
        var (db, project, gestor) = await SetupProjectWithGestor();
        await using var _ = db;

        var handler = new TransitionProjectStatusHandler(db);
        var cmd = new TransitionProjectStatusCommand(project.Id, ProjectStatus.Approved, gestor.Id);

        await handler.Handle(cmd, CancellationToken.None);

        var updated = await db.Projects.FindAsync(project.Id);
        updated!.Status.ShouldBe(ProjectStatus.Approved);
    }

    [Fact]
    public async Task Handle_InvalidTransition_ProposedToCompleted_ThrowsInvalidOperationException()
    {
        var (db, project, gestor) = await SetupProjectWithGestor();
        await using var _ = db;

        var handler = new TransitionProjectStatusHandler(db);
        var cmd = new TransitionProjectStatusCommand(project.Id, ProjectStatus.Completed, gestor.Id);

        await Should.ThrowAsync<InvalidOperationException>(
            () => handler.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Transition_ApprovedToInProgress_ByDesarrollador_ThrowsUnauthorizedAccessException()
    {
        await using var db = CreateInMemoryContext();

        var dev = Person.CreateFromClaims("sub-dev", "Dev User", "dev@test.com", PersonRole.Desarrollador);
        db.Persons.Add(dev);

        var project = Project.Create("Proyecto Test", null, "Unidad TIC", ProjectComplexity.Low, null, null, null);
        project.TransitionTo(ProjectStatus.Approved);
        db.Projects.Add(project);

        await db.SaveChangesAsync();

        var handler = new TransitionProjectStatusHandler(db);
        var cmd = new TransitionProjectStatusCommand(project.Id, ProjectStatus.InProgress, dev.Id);

        await Should.ThrowAsync<UnauthorizedAccessException>(
            () => handler.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Transition_ApprovedToInProgress_ByJefeEquipoNotAssigned_ThrowsUnauthorizedAccessException()
    {
        await using var db = CreateInMemoryContext();

        var jefe = Person.CreateFromClaims("sub-jefe", "Jefe User", "jefe@test.com", PersonRole.JefeEquipo);
        db.Persons.Add(jefe);

        // A different team that is NOT assigned to the project
        var otherTeam = Team.Create("Otro Equipo", null, null);
        db.Teams.Add(otherTeam);

        var project = Project.Create("Proyecto Test", null, "Unidad TIC", ProjectComplexity.Low, null, null, null);
        project.TransitionTo(ProjectStatus.Approved);
        db.Projects.Add(project);

        await db.SaveChangesAsync();

        // Jefe belongs to the other team but the project has no teams assigned
        var membership = PersonTeamMembership.Create(jefe.Id, otherTeam.Id);
        db.PersonTeamMemberships.Add(membership);
        await db.SaveChangesAsync();

        var handler = new TransitionProjectStatusHandler(db);
        var cmd = new TransitionProjectStatusCommand(project.Id, ProjectStatus.InProgress, jefe.Id);

        await Should.ThrowAsync<UnauthorizedAccessException>(
            () => handler.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Transition_ApprovedToInProgress_ByJefeEquipoAssigned_Succeeds()
    {
        await using var db = CreateInMemoryContext();

        var jefe = Person.CreateFromClaims("sub-jefe", "Jefe User", "jefe@test.com", PersonRole.JefeEquipo);
        db.Persons.Add(jefe);

        var team = Team.Create("Equipo Asignado", null, null);
        db.Teams.Add(team);

        var project = Project.Create("Proyecto Test", null, "Unidad TIC", ProjectComplexity.Low, null, null, null);
        project.TransitionTo(ProjectStatus.Approved);
        db.Projects.Add(project);

        await db.SaveChangesAsync();

        // Assign team to project and jefe to team
        var assignment = ProjectTeamAssignment.Create(project.Id, team.Id, true);
        db.ProjectTeamAssignments.Add(assignment);

        var membership = PersonTeamMembership.Create(jefe.Id, team.Id);
        db.PersonTeamMemberships.Add(membership);

        await db.SaveChangesAsync();

        var handler = new TransitionProjectStatusHandler(db);
        var cmd = new TransitionProjectStatusCommand(project.Id, ProjectStatus.InProgress, jefe.Id);

        await handler.Handle(cmd, CancellationToken.None);

        var updated = await db.Projects.FindAsync(project.Id);
        updated!.Status.ShouldBe(ProjectStatus.InProgress);
    }
}
