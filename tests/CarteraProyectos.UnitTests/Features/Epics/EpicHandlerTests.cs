using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Features.Epics;
using CarteraProyectos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace CarteraProyectos.UnitTests.Features.Epics;

public class EpicHandlerTests
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
        var project = Project.Create("Proyecto Test", null, "TIC", ProjectComplexity.Low, null, null, null);
        db.Projects.Add(project);
        await db.SaveChangesAsync();
        return (db, project);
    }

    // --- CreateEpic ---

    [Fact]
    public async Task CreateEpic_ValidCommand_CreatesEpicAndReturnsId()
    {
        var (db, project) = await DbWithProject();
        var handler = new CreateEpicHandler(db);

        var id = await handler.Handle(new CreateEpicCommand(project.Id, "Épica 1", null, 1, 0), CancellationToken.None);

        id.ShouldBeGreaterThan(0);
        var epic = await db.Epics.FindAsync(id);
        epic.ShouldNotBeNull();
        epic.Title.ShouldBe("Épica 1");
        epic.ProjectId.ShouldBe(project.Id);
    }

    [Fact]
    public async Task CreateEpic_ProjectNotFound_ThrowsKeyNotFoundException()
    {
        await using var db = CreateDb();
        var handler = new CreateEpicHandler(db);

        await Should.ThrowAsync<KeyNotFoundException>(
            () => handler.Handle(new CreateEpicCommand(999, "Épica X", null, 0, 0), CancellationToken.None));
    }

    // --- UpdateEpic ---

    [Fact]
    public async Task UpdateEpic_ValidCommand_UpdatesFields()
    {
        var (db, project) = await DbWithProject();
        var epic = Epic.Create(project.Id, "Título original", null, 0, 0);
        db.Epics.Add(epic);
        await db.SaveChangesAsync();

        var handler = new UpdateEpicHandler(db);
        await handler.Handle(new UpdateEpicCommand(epic.Id, "Título nuevo", "Desc", 2, 1), CancellationToken.None);

        var updated = await db.Epics.FindAsync(epic.Id);
        updated!.Title.ShouldBe("Título nuevo");
        updated.Description.ShouldBe("Desc");
        updated.Priority.ShouldBe(2);
        updated.SortOrder.ShouldBe(1);
    }

    [Fact]
    public async Task UpdateEpic_NotFound_ThrowsKeyNotFoundException()
    {
        await using var db = CreateDb();
        var handler = new UpdateEpicHandler(db);

        await Should.ThrowAsync<KeyNotFoundException>(
            () => handler.Handle(new UpdateEpicCommand(999, "X", null, 0, 0), CancellationToken.None));
    }

    // --- DeleteEpic ---

    [Fact]
    public async Task DeleteEpic_ExistingEpic_RemovesIt()
    {
        var (db, project) = await DbWithProject();
        var epic = Epic.Create(project.Id, "Épica a borrar", null, 0, 0);
        db.Epics.Add(epic);
        await db.SaveChangesAsync();

        var handler = new DeleteEpicHandler(db);
        await handler.Handle(new DeleteEpicCommand(epic.Id), CancellationToken.None);

        var deleted = await db.Epics.FindAsync(epic.Id);
        deleted.ShouldBeNull();
    }

    [Fact]
    public async Task DeleteEpic_NotFound_ThrowsKeyNotFoundException()
    {
        await using var db = CreateDb();
        var handler = new DeleteEpicHandler(db);

        await Should.ThrowAsync<KeyNotFoundException>(
            () => handler.Handle(new DeleteEpicCommand(999), CancellationToken.None));
    }

    // --- GetEpics ---

    [Fact]
    public async Task GetEpics_ReturnsPaged_OrderedBySortOrder()
    {
        var (db, project) = await DbWithProject();
        db.Epics.AddRange(
            Epic.Create(project.Id, "Épica C", null, 0, 2),
            Epic.Create(project.Id, "Épica A", null, 0, 0),
            Epic.Create(project.Id, "Épica B", null, 0, 1));
        await db.SaveChangesAsync();

        var handler = new GetEpicsHandler(db);
        var result = await handler.Handle(new GetEpicsQuery(project.Id, 1, 10), CancellationToken.None);

        result.Total.ShouldBe(3);
        result.Items[0].Title.ShouldBe("Épica A");
        result.Items[1].Title.ShouldBe("Épica B");
        result.Items[2].Title.ShouldBe("Épica C");
    }

    [Fact]
    public async Task GetEpics_Pagination_ReturnsCorrectPage()
    {
        var (db, project) = await DbWithProject();
        for (int i = 0; i < 5; i++)
            db.Epics.Add(Epic.Create(project.Id, $"Épica {i}", null, 0, i));
        await db.SaveChangesAsync();

        var handler = new GetEpicsHandler(db);
        var result = await handler.Handle(new GetEpicsQuery(project.Id, 2, 2), CancellationToken.None);

        result.Total.ShouldBe(5);
        result.Items.Count.ShouldBe(2);
        result.Page.ShouldBe(2);
    }
}
