using CarteraProyectos.Core.Features.Promoters;
using CarteraProyectos.Core.Interfaces;
using MediatR;

namespace CarteraProyectos.Api.Endpoints;

public static class PromoterEndpoints
{
    public static IEndpointRouteBuilder MapPromoterEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/promoters").RequireAuthorization();

        group.MapGet("/", async (IMediator mediator, CancellationToken ct, string? q = null, int page = 1, int pageSize = 20) =>
            Results.Ok(await mediator.Send(new GetPromotersQuery(q, page, pageSize), ct)))
            .WithName("GetPromoters")
            .WithDescription("Lista todos los promotores paginados. Filtrable por nombre con el parámetro 'q' (búsqueda parcial).");

        group.MapPost("/", async (CreatePromoterCommand cmd, HttpContext ctx, IAppDbContext db, IMediator mediator, CancellationToken ct) =>
        {
            var requester = await CurrentUser.ResolveAsync(ctx, db, ct);
            if (requester is null) return Results.Unauthorized();
            var id = await mediator.Send(cmd with { RequestingPersonId = requester.Id }, ct);
            return Results.Created($"/api/promoters/{id}", new { id });
        })
        .WithName("CreatePromoter")
        .WithDescription("Crea un nuevo promotor. Solo Gestor.");

        group.MapPut("/{id:int}", async (int id, CreatePromoterCommand body, HttpContext ctx, IAppDbContext db, IMediator mediator, CancellationToken ct) =>
        {
            var requester = await CurrentUser.ResolveAsync(ctx, db, ct);
            if (requester is null) return Results.Unauthorized();
            await mediator.Send(new UpdatePromoterCommand(id, body.Name, requester.Id), ct);
            return Results.NoContent();
        })
        .WithName("UpdatePromoter")
        .WithDescription("Actualiza un promotor. Solo Gestor.");

        group.MapDelete("/{id:int}", async (int id, HttpContext ctx, IAppDbContext db, IMediator mediator, CancellationToken ct) =>
        {
            var requester = await CurrentUser.ResolveAsync(ctx, db, ct);
            if (requester is null) return Results.Unauthorized();
            await mediator.Send(new DeletePromoterCommand(id, requester.Id), ct);
            return Results.NoContent();
        })
        .WithName("DeletePromoter")
        .WithDescription("Elimina un promotor. Falla si tiene proyectos asignados. Solo Gestor.");

        return app;
    }
}
