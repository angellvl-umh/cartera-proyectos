using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Features.Tags;
using CarteraProyectos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace CarteraProyectos.UnitTests.Features.Tags;

public class TagHandlerTests
{
    private static AppDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static async Task<Person> AddGestorAsync(AppDbContext db)
    {
        var gestor = Person.CreateFromClaims(Guid.NewGuid().ToString(), "gestor", "gestor@uni.es", PersonRole.Gestor);
        db.Persons.Add(gestor);
        await db.SaveChangesAsync();
        return gestor;
    }

    private static async Task<Person> AddDevAsync(AppDbContext db)
    {
        var dev = Person.CreateFromClaims(Guid.NewGuid().ToString(), "dev", "dev@uni.es", PersonRole.Desarrollador);
        db.Persons.Add(dev);
        await db.SaveChangesAsync();
        return dev;
    }

    // --- GetTags ---

    [Fact]
    public async Task GetTags_ReturnsAllOrderedByName()
    {
        await using var db = CreateDb();
        db.Tags.AddRange(Tag.Create("Zeta", "#FF0000"), Tag.Create("Alpha", "#00FF00"));
        await db.SaveChangesAsync();

        var result = await new GetTagsHandler(db).Handle(new GetTagsQuery(), CancellationToken.None);

        result.Count.ShouldBe(2);
        result[0].Name.ShouldBe("Alpha");
        result[1].Name.ShouldBe("Zeta");
    }

    [Fact]
    public async Task GetTags_Empty_ReturnsEmptyList()
    {
        await using var db = CreateDb();
        var result = await new GetTagsHandler(db).Handle(new GetTagsQuery(), CancellationToken.None);
        result.ShouldBeEmpty();
    }

    // --- CreateTag ---

    [Fact]
    public async Task CreateTag_ValidCommand_CreatesAndReturnsId()
    {
        await using var db = CreateDb();
        var gestor = await AddGestorAsync(db);

        var id = await new CreateTagHandler(db).Handle(
            new CreateTagCommand("Urgente", "#FF0000", gestor.Id), CancellationToken.None);

        id.ShouldBeGreaterThan(0);
        var tag = await db.Tags.FindAsync(id);
        tag.ShouldNotBeNull();
        tag.Name.ShouldBe("Urgente");
        tag.Color.ShouldBe("#FF0000");
    }

    [Fact]
    public async Task CreateTag_DuplicateName_ThrowsInvalidOperationException()
    {
        await using var db = CreateDb();
        var gestor = await AddGestorAsync(db);
        db.Tags.Add(Tag.Create("Urgente", null));
        await db.SaveChangesAsync();

        await Should.ThrowAsync<InvalidOperationException>(() =>
            new CreateTagHandler(db).Handle(
                new CreateTagCommand("Urgente", null, gestor.Id), CancellationToken.None));
    }

    [Fact]
    public async Task CreateTag_NonGestor_ThrowsUnauthorizedAccessException()
    {
        await using var db = CreateDb();
        var dev = await AddDevAsync(db);

        await Should.ThrowAsync<UnauthorizedAccessException>(() =>
            new CreateTagHandler(db).Handle(
                new CreateTagCommand("Urgente", null, dev.Id), CancellationToken.None));
    }

    // --- UpdateTag ---

    [Fact]
    public async Task UpdateTag_ValidCommand_UpdatesNameAndColor()
    {
        await using var db = CreateDb();
        var gestor = await AddGestorAsync(db);
        var tag = Tag.Create("Viejo", "#000000");
        db.Tags.Add(tag);
        await db.SaveChangesAsync();

        await new UpdateTagHandler(db).Handle(
            new UpdateTagCommand(tag.Id, "Nuevo", "#FFFFFF", gestor.Id), CancellationToken.None);

        var updated = await db.Tags.FindAsync(tag.Id);
        updated!.Name.ShouldBe("Nuevo");
        updated.Color.ShouldBe("#FFFFFF");
    }

    [Fact]
    public async Task UpdateTag_NotFound_ThrowsKeyNotFoundException()
    {
        await using var db = CreateDb();
        var gestor = await AddGestorAsync(db);

        await Should.ThrowAsync<KeyNotFoundException>(() =>
            new UpdateTagHandler(db).Handle(
                new UpdateTagCommand(999, "Nombre", null, gestor.Id), CancellationToken.None));
    }

    // --- DeleteTag ---

    [Fact]
    public async Task DeleteTag_ExistingTag_Removes()
    {
        await using var db = CreateDb();
        var gestor = await AddGestorAsync(db);
        var tag = Tag.Create("A borrar", null);
        db.Tags.Add(tag);
        await db.SaveChangesAsync();

        await new DeleteTagHandler(db).Handle(
            new DeleteTagCommand(tag.Id, gestor.Id), CancellationToken.None);

        (await db.Tags.FindAsync(tag.Id)).ShouldBeNull();
    }

    [Fact]
    public async Task DeleteTag_NotFound_ThrowsKeyNotFoundException()
    {
        await using var db = CreateDb();
        var gestor = await AddGestorAsync(db);

        await Should.ThrowAsync<KeyNotFoundException>(() =>
            new DeleteTagHandler(db).Handle(
                new DeleteTagCommand(999, gestor.Id), CancellationToken.None));
    }

    [Fact]
    public async Task DeleteTag_NonGestor_ThrowsUnauthorizedAccessException()
    {
        await using var db = CreateDb();
        var dev = await AddDevAsync(db);
        var tag = Tag.Create("Tag", null);
        db.Tags.Add(tag);
        await db.SaveChangesAsync();

        await Should.ThrowAsync<UnauthorizedAccessException>(() =>
            new DeleteTagHandler(db).Handle(
                new DeleteTagCommand(tag.Id, dev.Id), CancellationToken.None));
    }
}
