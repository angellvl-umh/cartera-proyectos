using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Features.Persons;
using CarteraProyectos.Core.Interfaces;
using CarteraProyectos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Shouldly;
using Xunit;

namespace CarteraProyectos.UnitTests.Features.Persons;

public class GetPersonsHandlerTests
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
    public async Task Handle_ByDefaultExcludesInactivePersons()
    {
        // Arrange
        var db = GetDbContext();
        var activePerson = Person.Create("Active User", "active@example.com", PersonRole.Desarrollador);
        var inactivePerson = Person.Create("Inactive User", "inactive@example.com", PersonRole.Desarrollador);
        inactivePerson.Deactivate();
        
        db.Persons.Add(activePerson);
        db.Persons.Add(inactivePerson);
        await db.SaveChangesAsync();

        var query = new GetPersonsQuery(Page: 1, PageSize: 20, IncludeInactive: false);
        var handler = new GetPersonsHandler(BuildService(db));

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Count.ShouldBe(1);
        result.Items[0].Name.ShouldBe("Active User");
        result.Items[0].IsActive.ShouldBeTrue();
        result.Total.ShouldBe(1);
    }

    [Fact]
    public async Task Handle_WithIncludeInactiveTrue_IncludesInactivePersons()
    {
        // Arrange
        var db = GetDbContext();
        var activePerson = Person.Create("Active User", "active@example.com", PersonRole.Desarrollador);
        var inactivePerson = Person.Create("Inactive User", "inactive@example.com", PersonRole.Desarrollador);
        inactivePerson.Deactivate();
        
        db.Persons.Add(activePerson);
        db.Persons.Add(inactivePerson);
        await db.SaveChangesAsync();

        var query = new GetPersonsQuery(Page: 1, PageSize: 20, IncludeInactive: true);
        var handler = new GetPersonsHandler(BuildService(db));

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Count.ShouldBe(2);
        result.Total.ShouldBe(2);
        result.Items.Any(p => !p.IsActive).ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_IncludesHasLoggedInFlag()
    {
        // Arrange
        var db = GetDbContext();
        var preRegistered = Person.Create("Pre-Registered", "prereg@example.com", PersonRole.Desarrollador);
        var loggedIn = Person.CreateFromClaims("sub123", "Logged In", "loggedin@example.com", PersonRole.Desarrollador);
        
        db.Persons.Add(preRegistered);
        db.Persons.Add(loggedIn);
        await db.SaveChangesAsync();

        var query = new GetPersonsQuery(Page: 1, PageSize: 20, IncludeInactive: true);
        var handler = new GetPersonsHandler(BuildService(db));

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        var preReg = result.Items.FirstOrDefault(p => p.Name == "Pre-Registered");
        preReg.ShouldNotBeNull();
        preReg.HasLoggedIn.ShouldBeFalse();

        var logged = result.Items.FirstOrDefault(p => p.Name == "Logged In");
        logged.ShouldNotBeNull();
        logged.HasLoggedIn.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_SupportsPagination()
    {
        // Arrange
        var db = GetDbContext();
        for (int i = 1; i <= 25; i++)
        {
            db.Persons.Add(Person.Create($"User {i:D2}", $"user{i:D2}@example.com", PersonRole.Desarrollador));
        }
        await db.SaveChangesAsync();

        var handler = new GetPersonsHandler(BuildService(db));

        var query1 = new GetPersonsQuery(Page: 1, PageSize: 10, IncludeInactive: false);

        // Act
        var page1 = await handler.Handle(query1, CancellationToken.None);
        
        var query2 = new GetPersonsQuery(Page: 2, PageSize: 10, IncludeInactive: false);
        var page2 = await handler.Handle(query2, CancellationToken.None);

        // Assert
        page1.Items.Count.ShouldBe(10);
        page2.Items.Count.ShouldBe(10);
        page1.Total.ShouldBe(25);
        page2.Total.ShouldBe(25);
        page1.Items[0].Name.ShouldNotBe(page2.Items[0].Name);
    }
}
