using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Features.Agent;
using CarteraProyectos.Core.Features.Persons;
using CarteraProyectos.Core.Interfaces;
using CarteraProyectos.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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

    private static ISender CreateSender(AppDbContext db)
    {
        var services = new ServiceCollection();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(AgentGetPersonsQuery).Assembly));
        services.AddScoped<IAppDbContext>(sp => db);
        // El agente nunca crea credenciales locales, pero el handler de CreatePerson
        // requiere IIdentityProviderService inyectado — registrar un stub que nunca se invoca.
        var idpStub = Substitute.For<IIdentityProviderService>();
        idpStub.CreateUserWithTemporaryPasswordAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new IdentityCredentialsResult(IdentityCredentialsStatus.Unavailable, null));
        services.AddScoped<IIdentityProviderService>(_ => idpStub);
        var sp = services.BuildServiceProvider();
        return sp.GetRequiredService<ISender>();
    }

    private static async Task<Person> AddPersonAsync(AppDbContext db, string email, PersonRole role)
    {
        var person = Person.CreateFromClaims(Guid.NewGuid().ToString(), email.Split('@')[0], email, role);
        db.Persons.Add(person);
        await db.SaveChangesAsync();
        return person;
    }

    [Fact]
    public async Task AgentGetPersons_RetornaPersonasActivas()
    {
        await using var db = CreateDb();
        var gestor = await AddPersonAsync(db, "gestor@uni.es", PersonRole.Gestor);
        var dev1 = await AddPersonAsync(db, "dev1@uni.es", PersonRole.Desarrollador);
        var dev2 = await AddPersonAsync(db, "dev2@uni.es", PersonRole.Desarrollador);

        var handler = new AgentGetPersonsHandler(CreateSender(db));
        var result = await handler.Handle(new AgentGetPersonsQuery(false), CancellationToken.None);

        result.Count.ShouldBe(3);
    }

    [Fact]
    public async Task AgentCreatePerson_Gestor_CreaPerson()
    {
        await using var db = CreateDb();
        var gestor = await AddPersonAsync(db, "gestor@uni.es", PersonRole.Gestor);

        var handler = new AgentCreatePersonHandler(CreateSender(db));
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

        var handler = new AgentCreatePersonHandler(CreateSender(db));

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

        var handler = new AgentCreatePersonHandler(CreateSender(db));

        await Should.ThrowAsync<InvalidOperationException>(() =>
            handler.Handle(
                new AgentCreatePersonCommand(gestor.Id, "Nueva Persona", "nueva@uni.es", "JefeEquipo"),
                CancellationToken.None));
    }

    [Fact]
    public async Task AgentUpdatePerson_Gestor_ActualizaPerson()
    {
        await using var db = CreateDb();
        var gestor = await AddPersonAsync(db, "gestor@uni.es", PersonRole.Gestor);
        var dev = await AddPersonAsync(db, "dev@uni.es", PersonRole.Desarrollador);

        var handler = new AgentUpdatePersonHandler(CreateSender(db));
        await handler.Handle(
            new AgentUpdatePersonCommand(gestor.Id, dev.Id, "Nuevo Nombre", "newemail@uni.es", "Gestor"),
            CancellationToken.None);

        var updated = await db.Persons.FindAsync(dev.Id);
        updated!.Name.ShouldBe("Nuevo Nombre");
        updated.Email.ShouldBe("newemail@uni.es");
        updated.Role.ShouldBe(PersonRole.Gestor);
    }

    [Fact]
    public async Task AgentSetPersonActive_Desactiva()
    {
        await using var db = CreateDb();
        var gestor = await AddPersonAsync(db, "gestor@uni.es", PersonRole.Gestor);
        var dev = await AddPersonAsync(db, "dev@uni.es", PersonRole.Desarrollador);

        var handler = new AgentSetPersonActiveHandler(CreateSender(db));
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

        var handler = new AgentSetPersonActiveHandler(CreateSender(db));

        await Should.ThrowAsync<InvalidOperationException>(() =>
            handler.Handle(
                new AgentSetPersonActiveCommand(gestor.Id, gestor.Id, false),
                CancellationToken.None));
    }
}
