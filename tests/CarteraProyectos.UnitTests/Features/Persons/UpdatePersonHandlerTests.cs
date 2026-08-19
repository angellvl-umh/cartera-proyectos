using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Features.Persons;
using CarteraProyectos.Core.Interfaces;
using CarteraProyectos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Shouldly;
using Xunit;

namespace CarteraProyectos.UnitTests.Features.Persons;

public class UpdatePersonHandlerTests
{
    private IAppDbContext GetDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"test_{Guid.NewGuid()}")
            .Options;
        return new AppDbContext(options);
    }

    private static PersonManagementService BuildService(IAppDbContext db)
        => new(db, Substitute.For<IIdentityProviderService>());

    [Fact]
    public async Task Handle_WithValidData_UpdatesPersonNameEmailAndRole()
    {
        // Arrange
        var db = GetDbContext();
        var requester = Person.CreateFromClaims("sub123", "Gestor User", "gestor@example.com", PersonRole.Gestor);
        var person = Person.Create("John Developer", "john@example.com", PersonRole.Desarrollador);
        db.Persons.Add(requester);
        db.Persons.Add(person);
        await db.SaveChangesAsync();

        var command = new UpdatePersonCommand(
            PersonId: person.Id,
            Name: "Jane Developer",
            Email: "jane@example.com",
            Role: PersonRole.JefeEquipo,
            RequestingPersonId: requester.Id);

        var handler = new UpdatePersonHandler(BuildService(db));

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var updated = await db.Persons.FindAsync(person.Id);
        updated.ShouldNotBeNull();
        updated.Name.ShouldBe("Jane Developer");
        updated.Email.ShouldBe("jane@example.com");
        updated.Role.ShouldBe(PersonRole.JefeEquipo);
    }

    [Fact]
    public async Task Handle_WhenPersonNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var db = GetDbContext();
        var requester = Person.CreateFromClaims("sub123", "Gestor User", "gestor@example.com", PersonRole.Gestor);
        db.Persons.Add(requester);
        await db.SaveChangesAsync();

        var command = new UpdatePersonCommand(
            PersonId: 9999,
            Name: "Jane Developer",
            Email: "jane@example.com",
            Role: PersonRole.JefeEquipo,
            RequestingPersonId: requester.Id);

        var handler = new UpdatePersonHandler(BuildService(db));

        // Act & Assert
        var exception = await Record.ExceptionAsync(() => handler.Handle(command, CancellationToken.None));
        exception.ShouldBeOfType<KeyNotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenEmailDuplicateFromAnotherPerson_ThrowsInvalidOperationException()
    {
        // Arrange
        var db = GetDbContext();
        var requester = Person.CreateFromClaims("sub123", "Gestor User", "gestor@example.com", PersonRole.Gestor);
        var person = Person.Create("John Developer", "john@example.com", PersonRole.Desarrollador);
        var other = Person.Create("Other User", "other@example.com", PersonRole.Desarrollador);
        db.Persons.Add(requester);
        db.Persons.Add(person);
        db.Persons.Add(other);
        await db.SaveChangesAsync();

        var command = new UpdatePersonCommand(
            PersonId: person.Id,
            Name: "Jane Developer",
            Email: "other@example.com",
            Role: PersonRole.Desarrollador,
            RequestingPersonId: requester.Id);

        var handler = new UpdatePersonHandler(BuildService(db));

        // Act & Assert
        var exception = await Record.ExceptionAsync(() => handler.Handle(command, CancellationToken.None));
        exception.ShouldBeOfType<InvalidOperationException>();
        exception.Message.ShouldContain("Ya existe otra persona con ese email");
    }

    [Fact]
    public async Task Handle_WhenRequesterIsNotGestor_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var db = GetDbContext();
        var requester = Person.CreateFromClaims("sub123", "Developer User", "dev@example.com", PersonRole.Desarrollador);
        var person = Person.Create("John Developer", "john@example.com", PersonRole.Desarrollador);
        db.Persons.Add(requester);
        db.Persons.Add(person);
        await db.SaveChangesAsync();

        var command = new UpdatePersonCommand(
            PersonId: person.Id,
            Name: "Jane Developer",
            Email: "jane@example.com",
            Role: PersonRole.Desarrollador,
            RequestingPersonId: requester.Id);

        var handler = new UpdatePersonHandler(BuildService(db));

        // Act & Assert
        var exception = await Record.ExceptionAsync(() => handler.Handle(command, CancellationToken.None));
        exception.ShouldBeOfType<UnauthorizedAccessException>();
    }
}
