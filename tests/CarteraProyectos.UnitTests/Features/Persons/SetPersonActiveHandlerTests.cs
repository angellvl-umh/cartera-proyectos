using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Features.Persons;
using CarteraProyectos.Core.Interfaces;
using CarteraProyectos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace CarteraProyectos.UnitTests.Features.Persons;

public class SetPersonActiveHandlerTests
{
    private IAppDbContext GetDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"test_{Guid.NewGuid()}")
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task Handle_DeactivatesActivePerson()
    {
        // Arrange
        var db = GetDbContext();
        var requester = Person.CreateFromClaims("sub123", "Gestor User", "gestor@example.com", PersonRole.Gestor);
        var person = Person.Create("John Developer", "john@example.com", PersonRole.Desarrollador);
        db.Persons.Add(requester);
        db.Persons.Add(person);
        await db.SaveChangesAsync();

        var command = new SetPersonActiveCommand(
            PersonId: person.Id,
            IsActive: false,
            RequestingPersonId: requester.Id);

        var handler = new SetPersonActiveHandler(db);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var updated = await db.Persons.FindAsync(person.Id);
        updated.ShouldNotBeNull();
        updated.IsActive.ShouldBeFalse();
    }

    [Fact]
    public async Task Handle_ReactivatesInactivePerson()
    {
        // Arrange
        var db = GetDbContext();
        var requester = Person.CreateFromClaims("sub123", "Gestor User", "gestor@example.com", PersonRole.Gestor);
        var person = Person.Create("John Developer", "john@example.com", PersonRole.Desarrollador);
        person.Deactivate();
        db.Persons.Add(requester);
        db.Persons.Add(person);
        await db.SaveChangesAsync();

        var command = new SetPersonActiveCommand(
            PersonId: person.Id,
            IsActive: true,
            RequestingPersonId: requester.Id);

        var handler = new SetPersonActiveHandler(db);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var updated = await db.Persons.FindAsync(person.Id);
        updated.ShouldNotBeNull();
        updated.IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_WhenPersonNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var db = GetDbContext();
        var requester = Person.CreateFromClaims("sub123", "Gestor User", "gestor@example.com", PersonRole.Gestor);
        db.Persons.Add(requester);
        await db.SaveChangesAsync();

        var command = new SetPersonActiveCommand(
            PersonId: 9999,
            IsActive: false,
            RequestingPersonId: requester.Id);

        var handler = new SetPersonActiveHandler(db);

        // Act & Assert
        var exception = await Record.ExceptionAsync(() => handler.Handle(command, CancellationToken.None));
        exception.ShouldBeOfType<KeyNotFoundException>();
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

        var command = new SetPersonActiveCommand(
            PersonId: person.Id,
            IsActive: false,
            RequestingPersonId: requester.Id);

        var handler = new SetPersonActiveHandler(db);

        // Act & Assert
        var exception = await Record.ExceptionAsync(() => handler.Handle(command, CancellationToken.None));
        exception.ShouldBeOfType<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Handle_WhenAttemptingToDeactivateSelf_ThrowsInvalidOperationException()
    {
        // Arrange
        var db = GetDbContext();
        var requester = Person.CreateFromClaims("sub123", "Gestor User", "gestor@example.com", PersonRole.Gestor);
        db.Persons.Add(requester);
        await db.SaveChangesAsync();

        var command = new SetPersonActiveCommand(
            PersonId: requester.Id,
            IsActive: false,
            RequestingPersonId: requester.Id);

        var handler = new SetPersonActiveHandler(db);

        // Act & Assert
        var exception = await Record.ExceptionAsync(() => handler.Handle(command, CancellationToken.None));
        exception.ShouldBeOfType<InvalidOperationException>();
        exception.Message.ShouldContain("No puedes desactivarte a ti mismo");
    }
}
