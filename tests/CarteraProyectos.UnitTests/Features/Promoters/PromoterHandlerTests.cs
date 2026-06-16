using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Features.Promoters;
using CarteraProyectos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace CarteraProyectos.UnitTests.Features.Promoters;

public class PromoterHandlerTests
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

    // --- GetPromoters ---

    [Fact]
    public async Task GetPromoters_ReturnsPaged()
    {
        await using var db = CreateDb();
        db.Promoters.AddRange(Promoter.Create("Beta"), Promoter.Create("Alpha"));
        await db.SaveChangesAsync();

        var result = await new GetPromotersHandler(db).Handle(new GetPromotersQuery(1, 10), CancellationToken.None);

        result.Total.ShouldBe(2);
        result.Items[0].Name.ShouldBe("Alpha"); // ordered by name
        result.Items[1].Name.ShouldBe("Beta");
    }

    // --- CreatePromoter ---

    [Fact]
    public async Task CreatePromoter_ValidCommand_CreatesAndReturnsId()
    {
        await using var db = CreateDb();
        var gestor = await AddGestorAsync(db);

        var id = await new CreatePromoterHandler(db).Handle(
            new CreatePromoterCommand("Rectorado", gestor.Id), CancellationToken.None);

        id.ShouldBeGreaterThan(0);
        var promoter = await db.Promoters.FindAsync(id);
        promoter.ShouldNotBeNull();
        promoter.Name.ShouldBe("Rectorado");
    }

    [Fact]
    public async Task CreatePromoter_DuplicateName_ThrowsInvalidOperationException()
    {
        await using var db = CreateDb();
        var gestor = await AddGestorAsync(db);
        db.Promoters.Add(Promoter.Create("Rectorado"));
        await db.SaveChangesAsync();

        await Should.ThrowAsync<InvalidOperationException>(() =>
            new CreatePromoterHandler(db).Handle(
                new CreatePromoterCommand("Rectorado", gestor.Id), CancellationToken.None));
    }

    [Fact]
    public async Task CreatePromoter_NonGestor_ThrowsUnauthorizedAccessException()
    {
        await using var db = CreateDb();
        var dev = await AddDevAsync(db);

        await Should.ThrowAsync<UnauthorizedAccessException>(() =>
            new CreatePromoterHandler(db).Handle(
                new CreatePromoterCommand("Rectorado", dev.Id), CancellationToken.None));
    }

    // --- UpdatePromoter ---

    [Fact]
    public async Task UpdatePromoter_ValidCommand_UpdatesName()
    {
        await using var db = CreateDb();
        var gestor = await AddGestorAsync(db);
        var promoter = Promoter.Create("Viejo Nombre");
        db.Promoters.Add(promoter);
        await db.SaveChangesAsync();

        await new UpdatePromoterHandler(db).Handle(
            new UpdatePromoterCommand(promoter.Id, "Nuevo Nombre", gestor.Id), CancellationToken.None);

        var updated = await db.Promoters.FindAsync(promoter.Id);
        updated!.Name.ShouldBe("Nuevo Nombre");
    }

    [Fact]
    public async Task UpdatePromoter_NotFound_ThrowsKeyNotFoundException()
    {
        await using var db = CreateDb();
        var gestor = await AddGestorAsync(db);

        await Should.ThrowAsync<KeyNotFoundException>(() =>
            new UpdatePromoterHandler(db).Handle(
                new UpdatePromoterCommand(999, "Nombre", gestor.Id), CancellationToken.None));
    }

    // --- DeletePromoter ---

    [Fact]
    public async Task DeletePromoter_NoProjects_Removes()
    {
        await using var db = CreateDb();
        var gestor = await AddGestorAsync(db);
        var promoter = Promoter.Create("A borrar");
        db.Promoters.Add(promoter);
        await db.SaveChangesAsync();

        await new DeletePromoterHandler(db).Handle(
            new DeletePromoterCommand(promoter.Id, gestor.Id), CancellationToken.None);

        (await db.Promoters.FindAsync(promoter.Id)).ShouldBeNull();
    }

    [Fact]
    public async Task DeletePromoter_WithProjects_ThrowsInvalidOperationException()
    {
        await using var db = CreateDb();
        var gestor = await AddGestorAsync(db);
        var promoter = Promoter.Create("Con proyectos");
        db.Promoters.Add(promoter);
        await db.SaveChangesAsync();

        var project = Project.Create("P1", null, null, ProjectComplexity.Small, null, null, null, promoterId: promoter.Id);
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        await Should.ThrowAsync<InvalidOperationException>(() =>
            new DeletePromoterHandler(db).Handle(
                new DeletePromoterCommand(promoter.Id, gestor.Id), CancellationToken.None));
    }

    [Fact]
    public async Task DeletePromoter_NotFound_ThrowsKeyNotFoundException()
    {
        await using var db = CreateDb();
        var gestor = await AddGestorAsync(db);

        await Should.ThrowAsync<KeyNotFoundException>(() =>
            new DeletePromoterHandler(db).Handle(
                new DeletePromoterCommand(999, gestor.Id), CancellationToken.None));
    }
}
