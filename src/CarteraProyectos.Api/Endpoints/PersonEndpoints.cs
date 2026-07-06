using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Features.Persons;
using CarteraProyectos.Core.Interfaces;
using MediatR;

namespace CarteraProyectos.Api.Endpoints;

public static class PersonEndpoints
{
    public static IEndpointRouteBuilder MapPersonEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/persons").RequireAuthorization();

        group.MapGet("/", async (IMediator mediator, CancellationToken ct, int page = 1, int pageSize = 20, bool includeInactive = false) =>
            Results.Ok(await mediator.Send(new GetPersonsQuery(page, pageSize, includeInactive), ct)))
            .WithName("GetPersons")
            .WithDescription("Lista todas las personas registradas en el sistema. Soporta paginación con page y pageSize (máx 100). El parámetro includeInactive (default false) filtra personas activas.");

        group.MapPost("/", async (CreatePersonRequest req, HttpContext ctx, IAppDbContext db, IMediator mediator, CancellationToken ct) =>
        {
            var requester = await CurrentUser.ResolveAsync(ctx, db, ct);
            if (requester is null) return Results.Unauthorized();

            if (!Enum.TryParse<PersonRole>(req.Role, out var role))
                return Results.BadRequest("Rol no válido. Valores aceptados: Desarrollador, JefeEquipo, Gestor.");

            var result = await mediator.Send(
                new CreatePersonCommand(req.Name, req.Email, role, requester.Id, req.CreateLocalCredentials ?? false), ct);

            return Results.Created($"/api/persons/{result.Id}", new
            {
                id = result.Id,
                temporaryPassword = result.TemporaryPassword,
                credentialsWarning = result.CredentialsWarning,
            });
        })
        .WithName("CreatePerson")
        .WithDescription(
            "Crea una persona pre-registrada que se vinculará con su cuenta SSO en su primer inicio de sesión. " +
            "Solo Gestor. Si createLocalCredentials es true, se crea también una cuenta usuario/contraseña en Keycloak " +
            "con contraseña temporal (el usuario deberá cambiarla en su primer inicio de sesión). " +
            "La respuesta incluye temporaryPassword (solo si se creó la cuenta) y credentialsWarning (si hubo algún aviso).");

        group.MapPut("/{id:int}", async (int id, UpdatePersonRequest req, HttpContext ctx, IAppDbContext db, IMediator mediator, CancellationToken ct) =>
        {
            var requester = await CurrentUser.ResolveAsync(ctx, db, ct);
            if (requester is null) return Results.Unauthorized();

            if (!Enum.TryParse<PersonRole>(req.Role, out var role))
                return Results.BadRequest("Rol no válido. Valores aceptados: Desarrollador, JefeEquipo, Gestor.");

            await mediator.Send(new UpdatePersonCommand(id, req.Name, req.Email, role, requester.Id), ct);
            return Results.NoContent();
        })
        .WithName("UpdatePerson")
        .WithDescription("Actualiza nombre, email y rol de una persona. Solo Gestor.");

        group.MapPut("/{id:int}/active", async (int id, SetPersonActiveRequest req, HttpContext ctx, IAppDbContext db, IMediator mediator, CancellationToken ct) =>
        {
            var requester = await CurrentUser.ResolveAsync(ctx, db, ct);
            if (requester is null) return Results.Unauthorized();

            await mediator.Send(new SetPersonActiveCommand(id, req.IsActive, requester.Id), ct);
            return Results.NoContent();
        })
        .WithName("SetPersonActive")
        .WithDescription("Activa o desactiva una persona. Las personas inactivas no aparecen en listados ni pueden recibir asignaciones. Solo Gestor.");

        return app;
    }
}

record CreatePersonRequest(string Name, string Email, string Role, bool? CreateLocalCredentials = false);
record UpdatePersonRequest(string Name, string Email, string Role);
record SetPersonActiveRequest(bool IsActive);
