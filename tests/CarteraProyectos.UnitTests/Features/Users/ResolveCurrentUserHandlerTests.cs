using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Features.Users;
using CarteraProyectos.Core.Interfaces;
using CarteraProyectos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace CarteraProyectos.UnitTests.Features.Users;

public class ResolveCurrentUserHandlerTests
{
    private IAppDbContext GetDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"test_{Guid.NewGuid()}")
            .Options;
        return new AppDbContext(options);
    }

    private static ResolveCurrentUserHandler BuildHandler(IAppDbContext db)
        => new(db);

    // ── Caso 1: Happy path — persona encontrada por SubjectId activa ────────

    [Fact]
    public async Task Handle_ExistingActivePersonBySubjectId_ReturnsOkWithData()
    {
        // Arrange
        var db = GetDbContext();
        var person = Person.CreateFromClaims("sub-001", "Ana García", "ana@example.com", PersonRole.Desarrollador);
        db.Persons.Add(person);
        await db.SaveChangesAsync();

        var command = new ResolveCurrentUserCommand("sub-001", "Ana García", "ana@example.com", []);
        var handler = BuildHandler(db);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Status.ShouldBe(ResolveUserStatus.Ok);
        result.Id.ShouldNotBeNull();
        result.Id!.Value.ShouldBeGreaterThan(0);
        result.SubjectId.ShouldBe("sub-001");
        result.Name.ShouldBe("Ana García");
        result.Email.ShouldBe("ana@example.com");
        result.IsActive.ShouldBe(true);
        result.Role.ShouldBe(nameof(PersonRole.Desarrollador));
    }

    // ── Caso 2: Vinculación — pre-registrada con SubjectId null ─────────────

    [Fact]
    public async Task Handle_PreregisteredPersonByEmail_LinksSubjectIdAndReturnsOk()
    {
        // Arrange
        var db = GetDbContext();
        var person = Person.Create("Luis López", "luis@example.com", PersonRole.Desarrollador);
        // SubjectId queda null en Create
        db.Persons.Add(person);
        await db.SaveChangesAsync();

        var command = new ResolveCurrentUserCommand("sub-new", "Luis López", "luis@example.com", []);
        var handler = BuildHandler(db);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Status.ShouldBe(ResolveUserStatus.Ok);
        result.SubjectId.ShouldBe("sub-new");

        // Verificar que el SubjectId fue persistido en BD
        var updated = await ((AppDbContext)db).Persons.FindAsync(person.Id);
        updated!.SubjectId.ShouldBe("sub-new");
    }

    // ── Caso 3: Re-vinculación — mismo email, SubjectId diferente (realm recreado) ─

    [Fact]
    public async Task Handle_PersonWithDifferentSubjectId_UpdatesSubjectIdAndReturnsOk()
    {
        // Arrange
        var db = GetDbContext();
        var person = Person.CreateFromClaims("old-sub", "Marta Ruiz", "marta@example.com", PersonRole.JefeEquipo);
        db.Persons.Add(person);
        await db.SaveChangesAsync();

        var command = new ResolveCurrentUserCommand("new-sub", "Marta Ruiz", "marta@example.com", []);
        var handler = BuildHandler(db);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Status.ShouldBe(ResolveUserStatus.Ok);
        result.SubjectId.ShouldBe("new-sub");

        var updated = await ((AppDbContext)db).Persons.FindAsync(person.Id);
        updated!.SubjectId.ShouldBe("new-sub");
    }

    // ── Caso 4: No registrada — sub y email desconocidos, no bootstrap ──────

    [Fact]
    public async Task Handle_UnknownEmailNotBootstrap_ReturnsNotRegisteredAndNoPersonCreated()
    {
        // Arrange
        var db = GetDbContext();
        // BD vacía — sin personas

        var command = new ResolveCurrentUserCommand(
            "sub-unknown", "Desconocido", "unknown@example.com",
            ["admin@example.com"]);
        var handler = BuildHandler(db);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Status.ShouldBe(ResolveUserStatus.NotRegistered);
        result.Id.ShouldBeNull();
        result.Name.ShouldBeNull();

        // No se debe haber creado ninguna Person
        var count = await ((AppDbContext)db).Persons.CountAsync();
        count.ShouldBe(0);
    }

    // ── Caso 5: Bootstrap — email en lista, insensible a mayúsculas ─────────

    [Fact]
    public async Task Handle_EmailInBootstrapList_CreatesGestorAndReturnsOk()
    {
        // Arrange
        var db = GetDbContext();
        var bootstrapEmail = "Admin@Universidad.es";

        var command = new ResolveCurrentUserCommand(
            "sub-admin", "Primer Gestor", "admin@universidad.es",
            [bootstrapEmail]);   // la comparación debe ser case-insensitive
        var handler = BuildHandler(db);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Status.ShouldBe(ResolveUserStatus.Ok);
        result.Role.ShouldBe(nameof(PersonRole.Gestor));
        result.Id.ShouldNotBeNull();

        // Verificar que la Person fue creada con rol Gestor
        var created = await ((AppDbContext)db).Persons.FindAsync(result.Id!.Value);
        created.ShouldNotBeNull();
        created.Role.ShouldBe(PersonRole.Gestor);
        created.SubjectId.ShouldBe("sub-admin");
    }

    // ── Caso 6: Inactiva por SubjectId ──────────────────────────────────────

    [Fact]
    public async Task Handle_InactivePersonBySubjectId_ReturnsInactive()
    {
        // Arrange
        var db = GetDbContext();
        var person = Person.CreateFromClaims("sub-inactive", "Carlos Ruiz", "carlos@example.com", PersonRole.Desarrollador);
        person.Deactivate();
        db.Persons.Add(person);
        await db.SaveChangesAsync();

        var command = new ResolveCurrentUserCommand("sub-inactive", "Carlos Ruiz", "carlos@example.com", []);
        var handler = BuildHandler(db);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Status.ShouldBe(ResolveUserStatus.Inactive);
        result.Id.ShouldBeNull();
    }

    // ── Caso 7: Inactiva pre-registrada (vinculación con persona inactiva) ───

    [Fact]
    public async Task Handle_InactivePreregisteredPerson_ReturnsInactive()
    {
        // Arrange
        var db = GetDbContext();
        var person = Person.Create("Eva Torres", "eva@example.com", PersonRole.Desarrollador);
        person.Deactivate();
        db.Persons.Add(person);
        await db.SaveChangesAsync();

        // Llega con un sub nuevo, se vincula por email, pero está inactiva
        var command = new ResolveCurrentUserCommand("sub-eva-new", "Eva Torres", "eva@example.com", []);
        var handler = BuildHandler(db);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Status.ShouldBe(ResolveUserStatus.Inactive);
        result.Id.ShouldBeNull();
    }
}
