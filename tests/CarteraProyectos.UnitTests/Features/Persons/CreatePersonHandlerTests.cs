using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Features.Persons;
using CarteraProyectos.Core.Interfaces;
using CarteraProyectos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace CarteraProyectos.UnitTests.Features.Persons;

public class CreatePersonHandlerTests
{
    private IAppDbContext GetDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"test_{Guid.NewGuid()}")
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task Handle_WithValidData_CreatesPersonWithSubjectIdNullAndIsActiveTrue()
    {
        // Arrange
        var db = GetDbContext();
        var requester = Person.CreateFromClaims("sub123", "Gestor User", "gestor@example.com", PersonRole.Gestor);
        db.Persons.Add(requester);
        await db.SaveChangesAsync();

        var command = new CreatePersonCommand(
            Name: "John Developer",
            Email: "john@example.com",
            Role: PersonRole.Desarrollador,
            RequestingPersonId: requester.Id);

        var handler = new CreatePersonHandler(db);

        // Act
        var personId = await handler.Handle(command, CancellationToken.None);

        // Assert
        personId.ShouldBeGreaterThan(0);
        var person = await db.Persons.FindAsync(personId);
        person.ShouldNotBeNull();
        person.Name.ShouldBe("John Developer");
        person.Email.ShouldBe("john@example.com");
        person.Role.ShouldBe(PersonRole.Desarrollador);
        person.SubjectId.ShouldBeNull();
        person.IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_WithDuplicateEmail_ThrowsInvalidOperationException()
    {
        // Arrange
        var db = GetDbContext();
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

        var handler = new CreatePersonHandler(db);

        // Act & Assert
        var exception = await Record.ExceptionAsync(() => handler.Handle(command, CancellationToken.None));
        exception.ShouldBeOfType<InvalidOperationException>();
        exception.Message.ShouldContain("Ya existe una persona con ese email");
    }

    [Fact]
    public async Task Handle_WhenRequesterIsNotGestor_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var db = GetDbContext();
        var requester = Person.CreateFromClaims("sub123", "Developer User", "dev@example.com", PersonRole.Desarrollador);
        db.Persons.Add(requester);
        await db.SaveChangesAsync();

        var command = new CreatePersonCommand(
            Name: "John Developer",
            Email: "john@example.com",
            Role: PersonRole.Desarrollador,
            RequestingPersonId: requester.Id);

        var handler = new CreatePersonHandler(db);

        // Act & Assert
        var exception = await Record.ExceptionAsync(() => handler.Handle(command, CancellationToken.None));
        exception.ShouldBeOfType<UnauthorizedAccessException>();
    }
}
