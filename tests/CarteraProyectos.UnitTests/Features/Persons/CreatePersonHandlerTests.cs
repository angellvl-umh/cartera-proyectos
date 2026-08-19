using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Features.Persons;
using CarteraProyectos.Core.Interfaces;
using CarteraProyectos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Shouldly;
using Xunit;

namespace CarteraProyectos.UnitTests.Features.Persons;

public class CreatePersonHandlerTests
{
    private static IAppDbContext GetDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"test_{Guid.NewGuid()}")
            .Options;
        return new AppDbContext(options);
    }

    private static IIdentityProviderService NoOpIdp()
    {
        var idp = Substitute.For<IIdentityProviderService>();
        // Por defecto nunca se llama (los tests sin flag lo verifican)
        return idp;
    }

    private static CreatePersonHandler BuildHandler(IAppDbContext db, IIdentityProviderService idp)
        => new(new PersonManagementService(db, idp));

    // ── Caso 1: sin flag → no se llama al IdP, resultado sin password ni warning ──

    [Fact]
    public async Task Handle_WithValidData_NoCreateLocalCredentials_CreatesPersonAndReturnsNoPasswordNoWarning()
    {
        // Arrange
        var db = GetDbContext();
        var idp = NoOpIdp();

        var requester = Person.CreateFromClaims("sub123", "Gestor User", "gestor@example.com", PersonRole.Gestor);
        db.Persons.Add(requester);
        await db.SaveChangesAsync();

        var command = new CreatePersonCommand(
            Name: "John Developer",
            Email: "john@example.com",
            Role: PersonRole.Desarrollador,
            RequestingPersonId: requester.Id,
            CreateLocalCredentials: false);

        var handler = BuildHandler(db, idp);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Id.ShouldBeGreaterThan(0);
        result.TemporaryPassword.ShouldBeNull();
        result.CredentialsWarning.ShouldBeNull();

        var person = await db.Persons.FindAsync(result.Id);
        person.ShouldNotBeNull();
        person.Name.ShouldBe("John Developer");
        person.Email.ShouldBe("john@example.com");
        person.Role.ShouldBe(PersonRole.Desarrollador);
        person.SubjectId.ShouldBeNull();
        person.IsActive.ShouldBeTrue();

        // El IdP no debe haberse llamado
        await idp.DidNotReceive().CreateUserWithTemporaryPasswordAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // Mantener compatibilidad: la forma sin el flag explícito también funciona

    [Fact]
    public async Task Handle_WithValidData_DefaultFlag_CreatesPersonWithSubjectIdNullAndIsActiveTrue()
    {
        // Arrange
        var db = GetDbContext();
        var idp = NoOpIdp();
        var requester = Person.CreateFromClaims("sub123", "Gestor User", "gestor@example.com", PersonRole.Gestor);
        db.Persons.Add(requester);
        await db.SaveChangesAsync();

        var command = new CreatePersonCommand(
            Name: "John Developer",
            Email: "john@example.com",
            Role: PersonRole.Desarrollador,
            RequestingPersonId: requester.Id);

        var handler = BuildHandler(db, idp);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Id.ShouldBeGreaterThan(0);
        var person = await db.Persons.FindAsync(result.Id);
        person.ShouldNotBeNull();
        person.Name.ShouldBe("John Developer");
        person.Email.ShouldBe("john@example.com");
        person.Role.ShouldBe(PersonRole.Desarrollador);
        person.SubjectId.ShouldBeNull();
        person.IsActive.ShouldBeTrue();
    }

    // ── Caso 2: con flag y Created → TemporaryPassword presente ──────────────

    [Fact]
    public async Task Handle_WithCreateLocalCredentials_AndCreated_ReturnsTemporaryPassword()
    {
        // Arrange
        var db = GetDbContext();
        var idp = Substitute.For<IIdentityProviderService>();
        idp.CreateUserWithTemporaryPasswordAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new IdentityCredentialsResult(IdentityCredentialsStatus.Created, "Abc123def!"));

        var requester = Person.CreateFromClaims("sub1", "Gestor", "gestor@example.com", PersonRole.Gestor);
        db.Persons.Add(requester);
        await db.SaveChangesAsync();

        var command = new CreatePersonCommand(
            Name: "Jane Developer",
            Email: "jane@example.com",
            Role: PersonRole.Desarrollador,
            RequestingPersonId: requester.Id,
            CreateLocalCredentials: true);

        var handler = BuildHandler(db, idp);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Id.ShouldBeGreaterThan(0);
        result.TemporaryPassword.ShouldBe("Abc123def!");
        result.CredentialsWarning.ShouldBeNull();

        // La Person se creó igualmente
        var person = await db.Persons.FindAsync(result.Id);
        person.ShouldNotBeNull();
        person.Email.ShouldBe("jane@example.com");
    }

    // ── Caso 3: con flag y AlreadyExists → warning sin password ──────────────

    [Fact]
    public async Task Handle_WithCreateLocalCredentials_AndAlreadyExists_ReturnsWarningNoPassword()
    {
        // Arrange
        var db = GetDbContext();
        var idp = Substitute.For<IIdentityProviderService>();
        idp.CreateUserWithTemporaryPasswordAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new IdentityCredentialsResult(IdentityCredentialsStatus.AlreadyExists, null));

        var requester = Person.CreateFromClaims("sub1", "Gestor", "gestor@example.com", PersonRole.Gestor);
        db.Persons.Add(requester);
        await db.SaveChangesAsync();

        var command = new CreatePersonCommand(
            Name: "Existing User",
            Email: "existing@example.com",
            Role: PersonRole.Desarrollador,
            RequestingPersonId: requester.Id,
            CreateLocalCredentials: true);

        var handler = BuildHandler(db, idp);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Id.ShouldBeGreaterThan(0);
        result.TemporaryPassword.ShouldBeNull();
        result.CredentialsWarning.ShouldNotBeNullOrWhiteSpace();
        result.CredentialsWarning.ShouldContain("Ya existía una cuenta");

        // La Person se creó igualmente
        var person = await db.Persons.FindAsync(result.Id);
        person.ShouldNotBeNull();
    }

    // ── Caso 4: con flag y Unavailable → Person creada + warning ─────────────

    [Fact]
    public async Task Handle_WithCreateLocalCredentials_AndUnavailable_CreatesPersonAndReturnsWarning()
    {
        // Arrange
        var db = GetDbContext();
        var idp = Substitute.For<IIdentityProviderService>();
        idp.CreateUserWithTemporaryPasswordAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new IdentityCredentialsResult(IdentityCredentialsStatus.Unavailable, null));

        var requester = Person.CreateFromClaims("sub1", "Gestor", "gestor@example.com", PersonRole.Gestor);
        db.Persons.Add(requester);
        await db.SaveChangesAsync();

        var command = new CreatePersonCommand(
            Name: "New Developer",
            Email: "newdev@example.com",
            Role: PersonRole.Desarrollador,
            RequestingPersonId: requester.Id,
            CreateLocalCredentials: true);

        var handler = BuildHandler(db, idp);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert — la Person SIEMPRE se crea aunque Keycloak falle
        result.Id.ShouldBeGreaterThan(0);
        result.TemporaryPassword.ShouldBeNull();
        result.CredentialsWarning.ShouldNotBeNullOrWhiteSpace();
        result.CredentialsWarning.ShouldContain("no se pudo crear la cuenta local");

        var person = await db.Persons.FindAsync(result.Id);
        person.ShouldNotBeNull();
        person.Email.ShouldBe("newdev@example.com");
    }

    // ── Caso 5a: email duplicado lanza excepción ──────────────────────────────

    [Fact]
    public async Task Handle_WithDuplicateEmail_ThrowsInvalidOperationException()
    {
        // Arrange
        var db = GetDbContext();
        var idp = NoOpIdp();
        var requester = Person.CreateFromClaims("sub123", "Gestor User", "gestor@example.com", PersonRole.Gestor);
        db.Persons.Add(requester);
        var existing = Person.Create("Existing", "john@example.com", PersonRole.Desarrollador);
        db.Persons.Add(existing);
        await db.SaveChangesAsync();

        var command = new CreatePersonCommand(
            Name: "John Developer",
            Email: "john@example.com",
            Role: PersonRole.Desarrollador,
            RequestingPersonId: requester.Id);

        var handler = BuildHandler(db, idp);

        // Act & Assert
        var exception = await Record.ExceptionAsync(() => handler.Handle(command, CancellationToken.None));
        exception.ShouldBeOfType<InvalidOperationException>();
        exception.Message.ShouldContain("Ya existe una persona con ese email");
    }

    // ── Caso 5b: requester no es Gestor lanza excepción ──────────────────────

    [Fact]
    public async Task Handle_WhenRequesterIsNotGestor_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var db = GetDbContext();
        var idp = NoOpIdp();
        var requester = Person.CreateFromClaims("sub123", "Developer User", "dev@example.com", PersonRole.Desarrollador);
        db.Persons.Add(requester);
        await db.SaveChangesAsync();

        var command = new CreatePersonCommand(
            Name: "John Developer",
            Email: "john@example.com",
            Role: PersonRole.Desarrollador,
            RequestingPersonId: requester.Id);

        var handler = BuildHandler(db, idp);

        // Act & Assert
        var exception = await Record.ExceptionAsync(() => handler.Handle(command, CancellationToken.None));
        exception.ShouldBeOfType<UnauthorizedAccessException>();
    }
}
