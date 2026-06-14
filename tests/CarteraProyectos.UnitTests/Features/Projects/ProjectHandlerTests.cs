using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Features.Projects;
using CarteraProyectos.Infrastructure.Persistence;
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

    // --- CreateProject ---

    [Fact]
    public async Task CreateProject_ValidCommand_CreatesProjectWithProposedStatus()
    {
        await using var db = CreateDb();
        var handler = new CreateProjectHandler(db);

        var id = await handler.Handle(
            new CreateProjectCommand("Portal Alumno", null, "RRHH", ProjectComplexity.Medium, 2026, null, null),
            CancellationToken.None);

        id.ShouldBeGreaterThan(0);
        var project = await db.Projects.FindAsync(id);
        project.ShouldNotBeNull();
        project.Title.ShouldBe("Portal Alumno");
        project.Status.ShouldBe(ProjectStatus.Proposed);
        project.RequestingUnit.ShouldBe("RRHH");
        project.Complexity.ShouldBe(ProjectComplexity.Medium);
    }

    // --- UpdateProject ---

    [Fact]
    public async Task UpdateProject_ValidCommand_UpdatesFields()
    {
        await using var db = CreateDb();
        var project = Project.Create("Original", null, "TIC", ProjectComplexity.Low, null, null, null);
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        var handler = new UpdateProjectHandler(db);
        await handler.Handle(
            new UpdateProjectCommand(project.Id, "Actualizado", "Nueva desc", "ADMIN", ProjectComplexity.High, 2027, null, null),
            CancellationToken.None);

        var updated = await db.Projects.FindAsync(project.Id);
        updated!.Title.ShouldBe("Actualizado");
        updated.Description.ShouldBe("Nueva desc");
        updated.RequestingUnit.ShouldBe("ADMIN");
        updated.Complexity.ShouldBe(ProjectComplexity.High);
    }

    [Fact]
    public async Task UpdateProject_NotFound_ThrowsKeyNotFoundException()
    {
        await using var db = CreateDb();
        var handler = new UpdateProjectHandler(db);

        await Should.ThrowAsync<KeyNotFoundException>(
            () => handler.Handle(
                new UpdateProjectCommand(999, "X", null, "TIC", ProjectComplexity.Low, null, null, null),
                CancellationToken.None));
    }

    // --- DeleteProject ---

    [Fact]
    public async Task DeleteProject_ExistingProject_RemovesIt()
    {
        await using var db = CreateDb();
        var project = Project.Create("A borrar", null, "TIC", ProjectComplexity.Low, null, null, null);
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

    // --- GetProjects ---

    [Fact]
    public async Task DeleteProject_InProgressProject_ThrowsInvalidOperationException()
    {
        await using var db = CreateDb();
        var project = Project.Create("Activo", null, "TIC", ProjectComplexity.Low, null, null, null);
        db.Projects.Add(project);
        await db.SaveChangesAsync();
        project.TransitionTo(ProjectStatus.Approved);
        project.TransitionTo(ProjectStatus.InProgress);
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
            Project.Create("P1", null, "TIC", ProjectComplexity.Low, null, null, null),
            Project.Create("P2", null, "TIC", ProjectComplexity.Low, null, null, null),
            Project.Create("P3", null, "TIC", ProjectComplexity.Low, null, null, null));
        await db.SaveChangesAsync();

        var handler = new GetProjectsHandler(db);
        var result = await handler.Handle(new GetProjectsQuery(Page: 1, PageSize: 2), CancellationToken.None);

        result.Total.ShouldBe(3);
        result.Items.Count.ShouldBe(2);
        result.Page.ShouldBe(1);
    }
}
