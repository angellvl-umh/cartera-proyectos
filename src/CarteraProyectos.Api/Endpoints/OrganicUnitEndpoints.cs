using CarteraProyectos.Core.Features.OrganicUnits;
using CarteraProyectos.Core.Interfaces;
using MediatR;

namespace CarteraProyectos.Api.Endpoints;

public static class OrganicUnitEndpoints
{
    public static IEndpointRouteBuilder MapOrganicUnitEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/organic-units").RequireAuthorization();

        group.MapGet("/", async (IMediator mediator, CancellationToken ct, string? q = null, int page = 1, int pageSize = 20) =>
            Results.Ok(await mediator.Send(new GetOrganicUnitsQuery(q, page, pageSize), ct)))
            .WithName("GetOrganicUnits")
            .WithDescription("Lista unidades orgánicas con búsqueda opcional por nombre o código.");

        group.MapPost("/", async (CreateOrganicUnitCommand cmd, HttpContext ctx, IAppDbContext db, IMediator mediator, CancellationToken ct) =>
        {
            var requester = await CurrentUser.ResolveAsync(ctx, db, ct);
            if (requester is null) return Results.Unauthorized();
            var id = await mediator.Send(cmd with { RequestingPersonId = requester.Id }, ct);
            return Results.Created($"/api/organic-units/{id}", new { id });
        })
        .WithName("CreateOrganicUnit")
        .WithDescription("Crea una nueva unidad orgánica. Solo Gestor.");

        group.MapPut("/{id:int}", async (int id, CreateOrganicUnitCommand body, HttpContext ctx, IAppDbContext db, IMediator mediator, CancellationToken ct) =>
        {
            var requester = await CurrentUser.ResolveAsync(ctx, db, ct);
            if (requester is null) return Results.Unauthorized();
            await mediator.Send(new UpdateOrganicUnitCommand(id, body.Name, body.Code, requester.Id), ct);
            return Results.NoContent();
        })
        .WithName("UpdateOrganicUnit")
        .WithDescription("Actualiza una unidad orgánica. Solo Gestor.");

        group.MapDelete("/{id:int}", async (int id, HttpContext ctx, IAppDbContext db, IMediator mediator, CancellationToken ct) =>
        {
            var requester = await CurrentUser.ResolveAsync(ctx, db, ct);
            if (requester is null) return Results.Unauthorized();
            await mediator.Send(new DeleteOrganicUnitCommand(id, requester.Id), ct);
            return Results.NoContent();
        })
        .WithName("DeleteOrganicUnit")
        .WithDescription("Elimina una unidad orgánica. Falla si tiene proyectos asignados. Solo Gestor.");

        return app;
    }
}
