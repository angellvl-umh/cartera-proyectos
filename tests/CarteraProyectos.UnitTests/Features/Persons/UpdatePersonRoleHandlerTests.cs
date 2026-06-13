using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Features.Persons;
using CarteraProyectos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace CarteraProyectos.UnitTests.Features.Persons;

public class UpdatePersonRoleHandlerTests
{
    private static AppDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task Handle_ChangeToDesarrollador_WhenPersonIsTeamLead_ThrowsInvalidOperationException()
    {
        await using var db = CreateInMemoryContext();

        var person = Person.CreateFromClaims("sub-1", "Jefe User", "jefe@test.com", PersonRole.JefeEquipo);
        db.Persons.Add(person);
        await db.SaveChangesAsync();

        // Create team with this person as lead
        var team = Team.Create("Equipo Liderado", null, person.Id);
        db.Teams.Add(team);
        await db.SaveChangesAsync();

        var handler = new UpdatePersonRoleHandler(db);
        var cmd = new UpdatePersonRoleCommand(person.Id, PersonRole.Desarrollador);

        await Should.ThrowAsync<InvalidOperationException>(
            () => handler.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ChangeRoleToJefeEquipo_UpdatesRoleSuccessfully()
    {
        await using var db = CreateInMemoryContext();

        var person = Person.CreateFromClaims("sub-2", "Dev User", "dev@test.com", PersonRole.Desarrollador);
        db.Persons.Add(person);
        await db.SaveChangesAsync();

        var handler = new UpdatePersonRoleHandler(db);
        var cmd = new UpdatePersonRoleCommand(person.Id, PersonRole.JefeEquipo);

        await handler.Handle(cmd, CancellationToken.None);

        var updated = await db.Persons.FindAsync(person.Id);
        updated!.Role.ShouldBe(PersonRole.JefeEquipo);
    }

    [Fact]
    public async Task Handle_ChangeRoleToGestor_UpdatesRoleSuccessfully()
    {
        await using var db = CreateInMemoryContext();

        var person = Person.CreateFromClaims("sub-3", "Staff User", "staff@test.com", PersonRole.Desarrollador);
        db.Persons.Add(person);
        await db.SaveChangesAsync();

        var handler = new UpdatePersonRoleHandler(db);
        var cmd = new UpdatePersonRoleCommand(person.Id, PersonRole.Gestor);

        await handler.Handle(cmd, CancellationToken.None);

        var updated = await db.Persons.FindAsync(person.Id);
        updated!.Role.ShouldBe(PersonRole.Gestor);
    }

    [Fact]
    public async Task Handle_ChangeToDesarrollador_WhenNotTeamLead_UpdatesRoleSuccessfully()
    {
        await using var db = CreateInMemoryContext();

        var person = Person.CreateFromClaims("sub-4", "Jefe User", "jefe2@test.com", PersonRole.JefeEquipo);
        db.Persons.Add(person);
        await db.SaveChangesAsync();

        // No team has this person as lead

        var handler = new UpdatePersonRoleHandler(db);
        var cmd = new UpdatePersonRoleCommand(person.Id, PersonRole.Desarrollador);

        await handler.Handle(cmd, CancellationToken.None);

        var updated = await db.Persons.FindAsync(person.Id);
        updated!.Role.ShouldBe(PersonRole.Desarrollador);
    }

    [Fact]
    public async Task Handle_PersonNotFound_ThrowsKeyNotFoundException()
    {
        await using var db = CreateInMemoryContext();
        var handler = new UpdatePersonRoleHandler(db);

        await Should.ThrowAsync<KeyNotFoundException>(
            () => handler.Handle(new UpdatePersonRoleCommand(999, PersonRole.Gestor), CancellationToken.None));
    }
}
