using CarteraProyectos.Core.Features.Teams;
using MediatR;

namespace CarteraProyectos.Api.Endpoints;

public static class TeamEndpoints
{
    public static IEndpointRouteBuilder MapTeamEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/teams").RequireAuthorization();

        group.MapGet("/", async (IMediator mediator, CancellationToken ct, int page = 1, int pageSize = 20) =>
            Results.Ok(await mediator.Send(new GetTeamsQuery(page, pageSize), ct)))
            .WithName("GetTeams")
            .WithDescription("Lista todos los equipos. Soporta paginación con page y pageSize (máx 100).");

        group.MapGet("/{id:int}", async (int id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetTeamQuery(id), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        })
        .WithName("GetTeam")
        .WithDescription("Devuelve el detalle de un equipo con sus miembros.");

        group.MapPost("/", async (CreateTeamCommand cmd, IMediator mediator, CancellationToken ct) =>
        {
            var id = await mediator.Send(cmd, ct);
            return Results.Created($"/api/teams/{id}", new { id });
        })
        .WithName("CreateTeam")
        .WithDescription("Crea un nuevo equipo. Solo Gestor.");

        group.MapPut("/{id:int}", async (int id, CreateTeamCommand body, IMediator mediator, CancellationToken ct) =>
        {
            await mediator.Send(new UpdateTeamCommand(id, body.Name, body.Description, body.LeadPersonId), ct);
            return Results.NoContent();
        })
        .WithName("UpdateTeam")
        .WithDescription("Actualiza un equipo existente. Solo Gestor.");

        group.MapDelete("/{id:int}", async (int id, IMediator mediator, CancellationToken ct) =>
        {
            await mediator.Send(new DeleteTeamCommand(id), ct);
            return Results.NoContent();
        })
        .WithName("DeleteTeam")
        .WithDescription("Elimina un equipo. Falla si tiene proyectos activos. Solo Gestor.");

        group.MapPost("/{id:int}/members", async (int id, AssignMemberRequest req, IMediator mediator, CancellationToken ct) =>
        {
            await mediator.Send(new AssignPersonToTeamCommand(id, req.PersonId), ct);
            return Results.NoContent();
        })
        .WithName("AssignPersonToTeam")
        .WithDescription("Asigna una persona a un equipo. Solo Gestor.");

        group.MapDelete("/{id:int}/members/{personId:int}", async (int id, int personId, IMediator mediator, CancellationToken ct) =>
        {
            await mediator.Send(new RemovePersonFromTeamCommand(id, personId), ct);
            return Results.NoContent();
        })
        .WithName("RemovePersonFromTeam")
        .WithDescription("Elimina a una persona de un equipo. Solo Gestor.");

        return app;
    }
}

record AssignMemberRequest(int PersonId);
