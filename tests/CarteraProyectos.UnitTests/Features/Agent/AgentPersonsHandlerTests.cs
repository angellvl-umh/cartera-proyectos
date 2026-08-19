using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Features.Agent;
using CarteraProyectos.Core.Features.Persons;
using CarteraProyectos.Core.Interfaces;
using CarteraProyectos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Shouldly;

namespace CarteraProyectos.UnitTests.Features.Agent;

public class AgentPersonsHandlerTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static IIdentityProviderService NoOpIdp()
    {
        var idp = Substitute.For<IIdentityProviderService>();
        // El agente nunca crea credenciales locales — stub que nunca debe invocarse
        idp.CreateUserWithTemporaryPasswordAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new IdentityCredentialsResult(IdentityCredentialsStatus.Unavailable, null));
        return idp;
    }

    private static IPersonManagementService BuildService(AppDbContext db)
        => new PersonManagementService(db, NoOpIdp());

    private static async Task<Person> AddPersonAsync(AppDbContext db, string email, PersonRole role)
    {
        var person = Person.CreateFromClaims(Guid.NewGuid().ToString(), email.Split('@')[0], email, role);
        db.Persons.Add(person);
        await db.SaveChangesAsync();
        return person;
    }

    // ─── AgentGetPersons ──────────────────────────────────────────────────────

    [Fact]
    public async Task AgentGetPersons_RetornaPersonasActivas()
    {
        await using var db = CreateDb();
        await AddPersonAsync(db, "gestor@uni.es", PersonRole.Gestor);
        await AddPersonAsync(db, "dev1@uni.es", PersonRole.Desarrollador);
        await AddPersonAsync(db, "dev2@uni.es", PersonRole.Desarrollador);

        var handler = new AgentGetPersonsHandler(BuildService(db));
        var result = await handler.Handle(new AgentGetPersonsQuery(false), CancellationToken.None);

        result.Count.ShouldBe(3);
    }

    [Fact]
    public async Task AgentGetPersons_IncludeInactive_DevuelveTodasLasPersonas()
    {
        await using var db = CreateDb();
        await AddPersonAsync(db, "gestor@uni.es", PersonRole.Gestor);
        var dev = await AddPersonAsync(db, "dev@uni.es", PersonRole.Desarrollador);
        dev.Deactivate();
        await db.SaveChangesAsync();

        var handler = new AgentGetPersonsHandler(BuildService(db));
        var result = await handler.Handle(new AgentGetPersonsQuery(true), CancellationToken.None);

        result.Count.ShouldBe(2);
    }

    // ─── AgentCreatePerson ────────────────────────────────────────────────────

    [Fact]
    public async Task AgentCreatePerson_Gestor_CreaPerson()
    {
        await using var db = CreateDb();
        var gestor = await AddPersonAsync(db, "gestor@uni.es", PersonRole.Gestor);

        var handler = new AgentCreatePersonHandler(BuildService(db));
        var id = await handler.Handle(
            new AgentCreatePersonCommand(gestor.Id, "Nueva Persona", "nueva@uni.es", "Desarrollador"),
            CancellationToken.None);

        var created = await db.Persons.FindAsync(id);
        created.ShouldNotBeNull();
        created.Name.ShouldBe("Nueva Persona");
        created.Email.ShouldBe("nueva@uni.es");
        created.Role.ShouldBe(PersonRole.Desarrollador);
    }

    [Fact]
    public async Task AgentCreatePerson_Desarrollador_LanzaUnauthorized()
    {
        await using var db = CreateDb();
        var dev = await AddPersonAsync(db, "dev@uni.es", PersonRole.Desarrollador);

        var handler = new AgentCreatePersonHandler(BuildService(db));

        await Should.ThrowAsync<UnauthorizedAccessException>(() =>
            handler.Handle(
                new AgentCreatePersonCommand(dev.Id, "Nueva Persona", "nueva@uni.es", "Desarrollador"),
                CancellationToken.None));
    }

    [Fact]
    public async Task AgentCreatePerson_RoleJefeEquipo_LanzaInvalidOperation()
    {
        await using var db = CreateDb();
        var gestor = await AddPersonAsync(db, "gestor@uni.es", PersonRole.Gestor);

        var handler = new AgentCreatePersonHandler(BuildService(db));

        await Should.ThrowAsync<InvalidOperationException>(() =>
            handler.Handle(
                new AgentCreatePersonCommand(gestor.Id, "Nueva Persona", "nueva@uni.es", "JefeEquipo"),
                CancellationToken.None));
    }

    // ─── AgentUpdatePerson ────────────────────────────────────────────────────

    [Fact]
    public async Task AgentUpdatePerson_Gestor_ActualizaPerson()
    {
        await using var db = CreateDb();
        var gestor = await AddPersonAsync(db, "gestor@uni.es", PersonRole.Gestor);
        var dev = await AddPersonAsync(db, "dev@uni.es", PersonRole.Desarrollador);

        var handler = new AgentUpdatePersonHandler(BuildService(db));
        await handler.Handle(
            new AgentUpdatePersonCommand(gestor.Id, dev.Id, "Nuevo Nombre", "newemail@uni.es", "Gestor"),
            CancellationToken.None);

        var updated = await db.Persons.FindAsync(dev.Id);
        updated!.Name.ShouldBe("Nuevo Nombre");
        updated.Email.ShouldBe("newemail@uni.es");
        updated.Role.ShouldBe(PersonRole.Gestor);
    }

    [Fact]
    public async Task AgentUpdatePerson_RoleJefeEquipo_LanzaInvalidOperation()
    {
        await using var db = CreateDb();
        var gestor = await AddPersonAsync(db, "gestor@uni.es", PersonRole.Gestor);
        var dev = await AddPersonAsync(db, "dev@uni.es", PersonRole.Desarrollador);

        var handler = new AgentUpdatePersonHandler(BuildService(db));

        await Should.ThrowAsync<InvalidOperationException>(() =>
            handler.Handle(
                new AgentUpdatePersonCommand(gestor.Id, dev.Id, "Nombre", "newemail@uni.es", "JefeEquipo"),
                CancellationToken.None));
    }

    [Fact]
    public async Task AgentUpdatePerson_Desarrollador_LanzaUnauthorized()
    {
        await using var db = CreateDb();
        var dev1 = await AddPersonAsync(db, "dev1@uni.es", PersonRole.Desarrollador);
        var dev2 = await AddPersonAsync(db, "dev2@uni.es", PersonRole.Desarrollador);

        var handler = new AgentUpdatePersonHandler(BuildService(db));

        await Should.ThrowAsync<UnauthorizedAccessException>(() =>
            handler.Handle(
                new AgentUpdatePersonCommand(dev1.Id, dev2.Id, "Nombre", "newemail@uni.es", "Desarrollador"),
                CancellationToken.None));
    }

    // ─── AgentSetPersonActive ─────────────────────────────────────────────────

    [Fact]
    public async Task AgentSetPersonActive_Desactiva()
    {
        await using var db = CreateDb();
        var gestor = await AddPersonAsync(db, "gestor@uni.es", PersonRole.Gestor);
        var dev = await AddPersonAsync(db, "dev@uni.es", PersonRole.Desarrollador);

        var handler = new AgentSetPersonActiveHandler(BuildService(db));
        await handler.Handle(
            new AgentSetPersonActiveCommand(gestor.Id, dev.Id, false),
            CancellationToken.None);

        var updated = await db.Persons.FindAsync(dev.Id);
        updated!.IsActive.ShouldBeFalse();
    }

    [Fact]
    public async Task AgentSetPersonActive_GestorAutoDesactivate_LanzaInvalidOperation()
    {
        await using var db = CreateDb();
        var gestor = await AddPersonAsync(db, "gestor@uni.es", PersonRole.Gestor);

        var handler = new AgentSetPersonActiveHandler(BuildService(db));

        await Should.ThrowAsync<InvalidOperationException>(() =>
            handler.Handle(
                new AgentSetPersonActiveCommand(gestor.Id, gestor.Id, false),
                CancellationToken.None));
    }

    [Fact]
    public async Task AgentSetPersonActive_Reactiva()
    {
        await using var db = CreateDb();
        var gestor = await AddPersonAsync(db, "gestor@uni.es", PersonRole.Gestor);
        var dev = await AddPersonAsync(db, "dev@uni.es", PersonRole.Desarrollador);
        dev.Deactivate();
        await db.SaveChangesAsync();

        var handler = new AgentSetPersonActiveHandler(BuildService(db));
        await handler.Handle(
            new AgentSetPersonActiveCommand(gestor.Id, dev.Id, true),
            CancellationToken.None);

        var updated = await db.Persons.FindAsync(dev.Id);
        updated!.IsActive.ShouldBeTrue();
    }
}
