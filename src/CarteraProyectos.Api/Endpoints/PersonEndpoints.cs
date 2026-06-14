using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Features.Persons;
using MediatR;

namespace CarteraProyectos.Api.Endpoints;

public static class PersonEndpoints
{
    public static IEndpointRouteBuilder MapPersonEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/persons").RequireAuthorization();

        group.MapGet("/", async (IMediator mediator, CancellationToken ct, int page = 1, int pageSize = 20) =>
            Results.Ok(await mediator.Send(new GetPersonsQuery(page, pageSize), ct)))
            .WithName("GetPersons")
            .WithDescription("Lista todas las personas registradas en el sistema. Soporta paginación con page y pageSize (máx 100).");

        group.MapPut("/{id:int}/role", async (int id, UpdatePersonRoleRequest req, IMediator mediator, CancellationToken ct) =>
        {
            if (!Enum.TryParse<PersonRole>(req.Role, out var role))
                return Results.BadRequest("Rol no válido. Valores aceptados: Desarrollador, JefeEquipo, Gestor.");
            await mediator.Send(new UpdatePersonRoleCommand(id, role), ct);
            return Results.NoContent();
        })
        .WithName("UpdatePersonRole")
        .WithDescription("Actualiza el rol de una persona. Solo accesible por el Gestor.");

        return app;
    }
}

record UpdatePersonRoleRequest(string Role);
