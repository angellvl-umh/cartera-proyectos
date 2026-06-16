using CarteraProyectos.Core.Features.Tags;
using CarteraProyectos.Core.Interfaces;
using MediatR;

namespace CarteraProyectos.Api.Endpoints;

public static class TagEndpoints
{
    public static IEndpointRouteBuilder MapTagEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/tags").RequireAuthorization();

        group.MapGet("/", async (IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new GetTagsQuery(), ct)))
            .WithName("GetTags")
            .WithDescription("Lista todas las etiquetas.");

        group.MapPost("/", async (CreateTagCommand cmd, HttpContext ctx, IAppDbContext db, IMediator mediator, CancellationToken ct) =>
        {
            var requester = await CurrentUser.ResolveAsync(ctx, db, ct);
            if (requester is null) return Results.Unauthorized();
            var id = await mediator.Send(cmd with { RequestingPersonId = requester.Id }, ct);
            return Results.Created($"/api/tags/{id}", new { id });
        })
        .WithName("CreateTag")
        .WithDescription("Crea una nueva etiqueta. Solo Gestor.");

        group.MapPut("/{id:int}", async (int id, CreateTagCommand body, HttpContext ctx, IAppDbContext db, IMediator mediator, CancellationToken ct) =>
        {
            var requester = await CurrentUser.ResolveAsync(ctx, db, ct);
            if (requester is null) return Results.Unauthorized();
            await mediator.Send(new UpdateTagCommand(id, body.Name, body.Color, requester.Id), ct);
            return Results.NoContent();
        })
        .WithName("UpdateTag")
        .WithDescription("Actualiza una etiqueta. Solo Gestor.");

        group.MapDelete("/{id:int}", async (int id, HttpContext ctx, IAppDbContext db, IMediator mediator, CancellationToken ct) =>
        {
            var requester = await CurrentUser.ResolveAsync(ctx, db, ct);
            if (requester is null) return Results.Unauthorized();
            await mediator.Send(new DeleteTagCommand(id, requester.Id), ct);
            return Results.NoContent();
        })
        .WithName("DeleteTag")
        .WithDescription("Elimina una etiqueta y la desasocia de todos los proyectos. Solo Gestor.");

        return app;
    }
}
