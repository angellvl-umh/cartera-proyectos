using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Features.OrganicUnits;
using CarteraProyectos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace CarteraProyectos.UnitTests.Features.OrganicUnits;

public class OrganicUnitHandlerTests
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

    // --- GetOrganicUnits ---

    [Fact]
    public async Task GetOrganicUnits_ReturnsPaged_OrderedByName()
    {
        await using var db = CreateDb();
        db.OrganicUnits.AddRange(
            OrganicUnit.Create("Servicio TIC", "TIC"),
            OrganicUnit.Create("Biblioteca", "BIB"));
        await db.SaveChangesAsync();

        var result = await new GetOrganicUnitsHandler(db).Handle(
            new GetOrganicUnitsQuery(null, 1, 10), CancellationToken.None);

        result.Total.ShouldBe(2);
        result.Items[0].Name.ShouldBe("Biblioteca");
        result.Items[1].Name.ShouldBe("Servicio TIC");
    }

    [Fact]
    public async Task GetOrganicUnits_WithSearchQuery_FiltersResults()
    {
        await using var db = CreateDb();
        db.OrganicUnits.AddRange(
            OrganicUnit.Create("Servicio TIC", "TIC"),
            OrganicUnit.Create("Biblioteca", "BIB"));
        await db.SaveChangesAsync();

        // Búsqueda por Code exacto (case-insensitive: "TIC" es el código)
        var result = await new GetOrganicUnitsHandler(db).Handle(
            new GetOrganicUnitsQuery("TIC", 1, 10), CancellationToken.None);

        result.Total.ShouldBe(1);
        result.Items[0].Name.ShouldBe("Servicio TIC");
    }

    [Fact]
    public async Task GetOrganicUnits_SearchByNameWithAccent_FindsAccentedNames()
    {
        // "promocion" (sin tilde) debe encontrar "Dirección de Promoción" (con tilde)
        await using var db = CreateDb();
        db.OrganicUnits.AddRange(
            OrganicUnit.Create("Dirección de Promoción", "DPR"),
            OrganicUnit.Create("Servicio TIC", "TIC"));
        await db.SaveChangesAsync();

        var result = await new GetOrganicUnitsHandler(db).Handle(
            new GetOrganicUnitsQuery("promocion", 1, 10), CancellationToken.None);

        result.Total.ShouldBe(1);
        result.Items[0].Name.ShouldBe("Dirección de Promoción");
    }

    [Fact]
    public async Task GetOrganicUnits_SearchByCodeWithAccent_FindsAccentedCodes()
    {
        // El código también puede tener acentos; la búsqueda debe encontrarlo normalizado.
        await using var db = CreateDb();
        db.OrganicUnits.AddRange(
            OrganicUnit.Create("Unidad de Gestión", "GESTIÓN"),
            OrganicUnit.Create("Servicio TIC", "TIC"));
        await db.SaveChangesAsync();

        var result = await new GetOrganicUnitsHandler(db).Handle(
            new GetOrganicUnitsQuery("gestion", 1, 10), CancellationToken.None);

        result.Total.ShouldBe(1);
        result.Items[0].Name.ShouldBe("Unidad de Gestión");
    }

    [Fact]
    public async Task GetOrganicUnits_SearchIsCaseInsensitive_ByName()
    {
        await using var db = CreateDb();
        db.OrganicUnits.AddRange(
            OrganicUnit.Create("Investigación e Innovación", "I+D"),
            OrganicUnit.Create("Servicio TIC", "TIC"));
        await db.SaveChangesAsync();

        var result = await new GetOrganicUnitsHandler(db).Handle(
            new GetOrganicUnitsQuery("INVESTIGACION", 1, 10), CancellationToken.None);

        result.Total.ShouldBe(1);
        result.Items[0].Name.ShouldBe("Investigación e Innovación");
    }

    [Fact]
    public async Task GetOrganicUnits_SearchIsCaseInsensitive_ByCode()
    {
        await using var db = CreateDb();
        db.OrganicUnits.AddRange(
            OrganicUnit.Create("Servicio TIC", "TIC"),
            OrganicUnit.Create("Biblioteca", "BIB"));
        await db.SaveChangesAsync();

        // "tic" en minúsculas debe encontrar el Code "TIC"
        var result = await new GetOrganicUnitsHandler(db).Handle(
            new GetOrganicUnitsQuery("tic", 1, 10), CancellationToken.None);

        result.Total.ShouldBe(1);
        result.Items[0].Name.ShouldBe("Servicio TIC");
    }

    [Fact]
    public async Task GetOrganicUnits_SearchMatchesNameOrCode()
    {
        // Una búsqueda que coincide por Name en uno y por Code en otro debe devolver ambos.
        await using var db = CreateDb();
        db.OrganicUnits.AddRange(
            OrganicUnit.Create("Unidad TIC", "UTI"),
            OrganicUnit.Create("Otro Servicio", "TIC"),
            OrganicUnit.Create("Biblioteca", "BIB"));
        await db.SaveChangesAsync();

        var result = await new GetOrganicUnitsHandler(db).Handle(
            new GetOrganicUnitsQuery("TIC", 1, 10), CancellationToken.None);

        result.Total.ShouldBe(2);
        result.Items.Select(x => x.Name)
            .ShouldBe(new[] { "Otro Servicio", "Unidad TIC" }); // orden por Name
    }

    [Fact]
    public async Task GetOrganicUnits_SearchWithQ_OrderByNameAndPaginates()
    {
        // Con Q presente, el orden por Name y la paginación deben funcionar correctamente.
        await using var db = CreateDb();
        db.OrganicUnits.AddRange(
            OrganicUnit.Create("Servicio C", "SC"),
            OrganicUnit.Create("Servicio A", "SA"),
            OrganicUnit.Create("Servicio B", "SB"),
            OrganicUnit.Create("Biblioteca", "BIB"));
        await db.SaveChangesAsync();

        var page1 = await new GetOrganicUnitsHandler(db).Handle(
            new GetOrganicUnitsQuery("servicio", 1, 2), CancellationToken.None);

        page1.Total.ShouldBe(3);
        page1.Items.Count.ShouldBe(2);
        page1.Items[0].Name.ShouldBe("Servicio A");
        page1.Items[1].Name.ShouldBe("Servicio B");

        var page2 = await new GetOrganicUnitsHandler(db).Handle(
            new GetOrganicUnitsQuery("servicio", 2, 2), CancellationToken.None);

        page2.Total.ShouldBe(3);
        page2.Items.Count.ShouldBe(1);
        page2.Items[0].Name.ShouldBe("Servicio C");
    }

    // --- CreateOrganicUnit ---

    [Fact]
    public async Task CreateOrganicUnit_ValidCommand_CreatesAndReturnsId()
    {
        await using var db = CreateDb();
        var gestor = await AddGestorAsync(db);

        var id = await new CreateOrganicUnitHandler(db).Handle(
            new CreateOrganicUnitCommand("Servicio TIC", "TIC", gestor.Id), CancellationToken.None);

        id.ShouldBeGreaterThan(0);
        var unit = await db.OrganicUnits.FindAsync(id);
        unit.ShouldNotBeNull();
        unit.Name.ShouldBe("Servicio TIC");
        unit.Code.ShouldBe("TIC");
    }

    [Fact]
    public async Task CreateOrganicUnit_DuplicateName_ThrowsInvalidOperationException()
    {
        await using var db = CreateDb();
        var gestor = await AddGestorAsync(db);
        db.OrganicUnits.Add(OrganicUnit.Create("Servicio TIC", "TIC"));
        await db.SaveChangesAsync();

        await Should.ThrowAsync<InvalidOperationException>(() =>
            new CreateOrganicUnitHandler(db).Handle(
                new CreateOrganicUnitCommand("Servicio TIC", null, gestor.Id), CancellationToken.None));
    }

    [Fact]
    public async Task CreateOrganicUnit_NonGestor_ThrowsUnauthorizedAccessException()
    {
        await using var db = CreateDb();
        var dev = await AddDevAsync(db);

        await Should.ThrowAsync<UnauthorizedAccessException>(() =>
            new CreateOrganicUnitHandler(db).Handle(
                new CreateOrganicUnitCommand("TIC", null, dev.Id), CancellationToken.None));
    }

    // --- UpdateOrganicUnit ---

    [Fact]
    public async Task UpdateOrganicUnit_ValidCommand_UpdatesFields()
    {
        await using var db = CreateDb();
        var gestor = await AddGestorAsync(db);
        var unit = OrganicUnit.Create("Viejo", "VIE");
        db.OrganicUnits.Add(unit);
        await db.SaveChangesAsync();

        await new UpdateOrganicUnitHandler(db).Handle(
            new UpdateOrganicUnitCommand(unit.Id, "Nuevo", "NUE", gestor.Id), CancellationToken.None);

        var updated = await db.OrganicUnits.FindAsync(unit.Id);
        updated!.Name.ShouldBe("Nuevo");
        updated.Code.ShouldBe("NUE");
    }

    [Fact]
    public async Task UpdateOrganicUnit_NotFound_ThrowsKeyNotFoundException()
    {
        await using var db = CreateDb();
        var gestor = await AddGestorAsync(db);

        await Should.ThrowAsync<KeyNotFoundException>(() =>
            new UpdateOrganicUnitHandler(db).Handle(
                new UpdateOrganicUnitCommand(999, "Nombre", null, gestor.Id), CancellationToken.None));
    }

    // --- DeleteOrganicUnit ---

    [Fact]
    public async Task DeleteOrganicUnit_NoProjects_Removes()
    {
        await using var db = CreateDb();
        var gestor = await AddGestorAsync(db);
        var unit = OrganicUnit.Create("A borrar", null);
        db.OrganicUnits.Add(unit);
        await db.SaveChangesAsync();

        await new DeleteOrganicUnitHandler(db).Handle(
            new DeleteOrganicUnitCommand(unit.Id, gestor.Id), CancellationToken.None);

        (await db.OrganicUnits.FindAsync(unit.Id)).ShouldBeNull();
    }

    [Fact]
    public async Task DeleteOrganicUnit_WithProjects_ThrowsInvalidOperationException()
    {
        await using var db = CreateDb();
        var gestor = await AddGestorAsync(db);
        var unit = OrganicUnit.Create("Con proyectos", null);
        db.OrganicUnits.Add(unit);
        await db.SaveChangesAsync();

        var project = Project.Create("P1", null, null, ProjectComplexity.Small, null, null, null, organicUnitId: unit.Id);
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        await Should.ThrowAsync<InvalidOperationException>(() =>
            new DeleteOrganicUnitHandler(db).Handle(
                new DeleteOrganicUnitCommand(unit.Id, gestor.Id), CancellationToken.None));
    }

    [Fact]
    public async Task DeleteOrganicUnit_NotFound_ThrowsKeyNotFoundException()
    {
        await using var db = CreateDb();
        var gestor = await AddGestorAsync(db);

        await Should.ThrowAsync<KeyNotFoundException>(() =>
            new DeleteOrganicUnitHandler(db).Handle(
                new DeleteOrganicUnitCommand(999, gestor.Id), CancellationToken.None));
    }
}
