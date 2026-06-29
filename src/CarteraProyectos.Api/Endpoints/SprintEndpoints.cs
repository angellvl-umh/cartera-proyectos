using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Features.Sprints;
using CarteraProyectos.Core.Interfaces;
using MediatR;

namespace CarteraProyectos.Api.Endpoints;

public static class SprintEndpoints
{
    public static IEndpointRouteBuilder MapSprintEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/projects/{projectId:int}/sprints").RequireAuthorization();

        group.MapGet("/", async (int projectId, IMediator mediator, CancellationToken ct,
            int page = 1, int pageSize = 20) =>
            Results.Ok(await mediator.Send(new GetSprintsQuery(projectId, page, pageSize), ct)))
        .WithName("GetSprints")
        .WithDescription("Lista los sprints de un proyecto ordenados por fecha de inicio descendente.");

        group.MapGet("/{id:int}", async (int projectId, int id, IMediator mediator, CancellationToken ct) =>
        {
            try { return Results.Ok(await mediator.Send(new GetSprintByIdQuery(projectId, id), ct)); }
            catch (KeyNotFoundException) { return Results.NotFound(); }
        })
        .WithName("GetSprintById")
        .WithDescription("Devuelve el detalle de un sprint.");

        group.MapPost("/", async (int projectId, SprintRequest req, HttpContext ctx, IAppDbContext db, IMediator mediator, CancellationToken ct) =>
        {
            var requester = await CurrentUser.ResolveAsync(ctx, db, ct);
            if (requester is null) return Results.Unauthorized();

            var id = await mediator.Send(new CreateSprintCommand(
                projectId, req.Name, req.Goal, req.StartDate, req.EndDate, req.Capacity, requester.Id), ct);
            return Results.Created($"/api/projects/{projectId}/sprints/{id}", new { id });
        })
        .WithName("CreateSprint")
        .WithDescription("Crea un nuevo sprint en estado Planning.");

        group.MapPut("/{id:int}", async (int projectId, int id, SprintRequest req, IMediator mediator, CancellationToken ct) =>
        {
            await mediator.Send(new UpdateSprintCommand(id, req.Name, req.Goal, req.StartDate, req.EndDate, req.Capacity), ct);
            return Results.NoContent();
        })
        .WithName("UpdateSprint")
        .WithDescription("Actualiza un sprint. Solo se puede editar en estado Planning.");

        group.MapDelete("/{id:int}", async (int projectId, int id, IMediator mediator, CancellationToken ct) =>
        {
            await mediator.Send(new DeleteSprintCommand(id), ct);
            return Results.NoContent();
        })
        .WithName("DeleteSprint")
        .WithDescription("Elimina un sprint. Solo se puede eliminar en estado Planning.");

        group.MapPost("/{id:int}/status", async (int projectId, int id, TransitionSprintRequest req, HttpContext ctx, IAppDbContext db, IMediator mediator, CancellationToken ct) =>
        {
            if (!Enum.TryParse<SprintStatus>(req.Status, out var newStatus))
                return Results.BadRequest("Estado no válido. Valores: Planning, Active, Completed.");

            var requester = await CurrentUser.ResolveAsync(ctx, db, ct);
            if (requester is null) return Results.Unauthorized();

            await mediator.Send(new TransitionSprintStatusCommand(id, newStatus, requester.Id), ct);
            return Results.NoContent();
        })
        .WithName("TransitionSprintStatus")
        .WithDescription("Cambia el estado del sprint: Planning → Active → Completed. Solo un sprint activo por proyecto.");

        group.MapGet("/{id:int}/status-history", async (int projectId, int id, IMediator mediator, CancellationToken ct) =>
        {
            try { return Results.Ok(await mediator.Send(new GetSprintStatusHistoryQuery(projectId, id), ct)); }
            catch (KeyNotFoundException) { return Results.NotFound(); }
        })
        .WithName("GetSprintStatusHistory")
        .WithDescription("Devuelve el histórico de cambios de estado de un sprint, ordenado cronológicamente.");

        return app;
    }
}

record SprintRequest(string Name, string? Goal, DateOnly? StartDate, DateOnly? EndDate, int? Capacity);
record TransitionSprintRequest(string Status);
