using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Features.Projects;
using CarteraProyectos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace CarteraProyectos.UnitTests.Features.Projects;

public class GetProjectStatusHistoryHandlerTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static async Task<(AppDbContext db, Project project)> DbWithProject()
    {
        var db = CreateDb();
        var project = Project.Create("Proyecto Test", null, "TIC", ProjectComplexity.VerySmall, null, null, null);
        db.Projects.Add(project);
        await db.SaveChangesAsync();
        return (db, project);
    }

    // --- GetProjectStatusHistory ---

    [Fact]
    public async Task GetProjectStatusHistory_ReturnsEntriesOrderedByDate()
    {
        var (db, project) = await DbWithProject();
        var person = Person.CreateFromClaims("sub-h1", "Historiador", "hist@test.com", PersonRole.Gestor);
        db.Persons.Add(person);
        await db.SaveChangesAsync();

        var transitionHandler = new TransitionProjectStatusHandler(db);
        await transitionHandler.Handle(new TransitionProjectStatusCommand(project.Id, ProjectStatus.PlanningWithClient, person.Id), CancellationToken.None);
        await transitionHandler.Handle(new TransitionProjectStatusCommand(project.Id, ProjectStatus.PlanningSprint, person.Id), CancellationToken.None);

        var handler = new GetProjectStatusHistoryHandler(db);
        var result = await handler.Handle(new GetProjectStatusHistoryQuery(project.Id), CancellationToken.None);

        result.Count.ShouldBe(2);
        result[0].ToStatus.ShouldBe("PlanningWithClient");
        result[1].ToStatus.ShouldBe("PlanningSprint");
        result[1].ChangedByName.ShouldBe("Historiador");
    }

    [Fact]
    public async Task GetProjectStatusHistory_ProjectNotFound_ThrowsKeyNotFoundException()
    {
        await using var db = CreateDb();
        var handler = new GetProjectStatusHistoryHandler(db);

        await Should.ThrowAsync<KeyNotFoundException>(
            () => handler.Handle(new GetProjectStatusHistoryQuery(999), CancellationToken.None));
    }
}
