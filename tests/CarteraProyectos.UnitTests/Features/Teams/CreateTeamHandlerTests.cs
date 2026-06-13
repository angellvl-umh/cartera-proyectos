using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Features.Teams;
using CarteraProyectos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace CarteraProyectos.UnitTests.Features.Teams;

public class CreateTeamHandlerTests
{
    private static AppDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesTeamAndReturnsId()
    {
        await using var db = CreateInMemoryContext();
        var handler = new CreateTeamHandler(db);

        var id = await handler.Handle(new CreateTeamCommand("Equipo Alpha", "Descripción", null), CancellationToken.None);

        id.ShouldBeGreaterThan(0);
        var team = await db.Teams.FindAsync(id);
        team.ShouldNotBeNull();
        team.Name.ShouldBe("Equipo Alpha");
    }

    [Fact]
    public async Task Handle_DuplicateName_ThrowsInvalidOperationException()
    {
        await using var db = CreateInMemoryContext();
        db.Teams.Add(Team.Create("Equipo Alpha", null, null));
        await db.SaveChangesAsync();

        var handler = new CreateTeamHandler(db);

        await Should.ThrowAsync<InvalidOperationException>(
            () => handler.Handle(new CreateTeamCommand("Equipo Alpha", null, null), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_DesarrolladorAsLead_ThrowsInvalidOperationException()
    {
        await using var db = CreateInMemoryContext();
        var dev = Person.CreateFromClaims("sub-1", "Dev User", "dev@test.com", PersonRole.Desarrollador);
        db.Persons.Add(dev);
        await db.SaveChangesAsync();

        var handler = new CreateTeamHandler(db);

        await Should.ThrowAsync<InvalidOperationException>(
            () => handler.Handle(new CreateTeamCommand("Nuevo Equipo", null, dev.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_JefeEquipoAsLead_CreatesTeamSuccessfully()
    {
        await using var db = CreateInMemoryContext();
        var jefe = Person.CreateFromClaims("sub-2", "Jefe User", "jefe@test.com", PersonRole.JefeEquipo);
        db.Persons.Add(jefe);
        await db.SaveChangesAsync();

        var handler = new CreateTeamHandler(db);

        var id = await handler.Handle(new CreateTeamCommand("Equipo Beta", null, jefe.Id), CancellationToken.None);

        id.ShouldBeGreaterThan(0);
    }
}
