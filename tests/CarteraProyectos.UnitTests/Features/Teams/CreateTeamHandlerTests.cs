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

    private static async Task<(AppDbContext db, Person gestor)> DbWithGestor()
    {
        var db = CreateInMemoryContext();
        var gestor = Person.CreateFromClaims("sub-gestor", "Gestor", "gestor@test.com", PersonRole.Gestor);
        db.Persons.Add(gestor);
        await db.SaveChangesAsync();
        return (db, gestor);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesTeamAndReturnsId()
    {
        var (db, gestor) = await DbWithGestor();
        await using var _ = db;
        var handler = new CreateTeamHandler(db);

        var id = await handler.Handle(
            new CreateTeamCommand("Equipo Alpha", "Descripción", null, gestor.Id),
            CancellationToken.None);

        id.ShouldBeGreaterThan(0);
        var team = await db.Teams.FindAsync(id);
        team.ShouldNotBeNull();
        team.Name.ShouldBe("Equipo Alpha");
    }

    [Fact]
    public async Task Handle_DuplicateName_ThrowsInvalidOperationException()
    {
        var (db, gestor) = await DbWithGestor();
        await using var _ = db;
        db.Teams.Add(Team.Create("Equipo Alpha", null, null));
        await db.SaveChangesAsync();

        var handler = new CreateTeamHandler(db);

        await Should.ThrowAsync<InvalidOperationException>(
            () => handler.Handle(
                new CreateTeamCommand("Equipo Alpha", null, null, gestor.Id),
                CancellationToken.None));
    }

    [Fact]
    public async Task Handle_DesarrolladorAsLead_ThrowsInvalidOperationException()
    {
        var (db, gestor) = await DbWithGestor();
        await using var _ = db;
        var dev = Person.CreateFromClaims("sub-1", "Dev User", "dev@test.com", PersonRole.Desarrollador);
        db.Persons.Add(dev);
        await db.SaveChangesAsync();

        var handler = new CreateTeamHandler(db);

        await Should.ThrowAsync<InvalidOperationException>(
            () => handler.Handle(
                new CreateTeamCommand("Nuevo Equipo", null, dev.Id, gestor.Id),
                CancellationToken.None));
    }

    [Fact]
    public async Task Handle_JefeEquipoAsLead_CreatesTeamSuccessfully()
    {
        var (db, gestor) = await DbWithGestor();
        await using var _ = db;
        var jefe = Person.CreateFromClaims("sub-2", "Jefe User", "jefe@test.com", PersonRole.JefeEquipo);
        db.Persons.Add(jefe);
        await db.SaveChangesAsync();

        var handler = new CreateTeamHandler(db);

        var id = await handler.Handle(
            new CreateTeamCommand("Equipo Beta", null, jefe.Id, gestor.Id),
            CancellationToken.None);

        id.ShouldBeGreaterThan(0);
    }
}
