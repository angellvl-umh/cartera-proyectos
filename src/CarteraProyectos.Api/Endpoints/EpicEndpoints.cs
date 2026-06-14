using CarteraProyectos.Core.Features.Epics;
using MediatR;

namespace CarteraProyectos.Api.Endpoints;

public static class EpicEndpoints
{
    public static IEndpointRouteBuilder MapEpicEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/projects/{projectId:int}/epics").RequireAuthorization();

        group.MapGet("/", async (int projectId, IMediator mediator, CancellationToken ct, int page = 1, int pageSize = 50) =>
            Results.Ok(await mediator.Send(new GetEpicsQuery(projectId, page, pageSize), ct)))
            .WithName("GetEpics")
            .WithDescription("Lista las épicas de un proyecto ordenadas por SortOrder.");

        group.MapPost("/", async (int projectId, CreateEpicRequest req, IMediator mediator, CancellationToken ct) =>
        {
            var id = await mediator.Send(new CreateEpicCommand(
                projectId, req.Title, req.Description, req.Priority, req.SortOrder,
                req.EstimationHours, req.EstimationPoints), ct);
            return Results.Created($"/api/projects/{projectId}/epics/{id}", new { id });
        })
        .WithName("CreateEpic")
        .WithDescription("Crea una épica en el proyecto.");

        group.MapPut("/{id:int}", async (int projectId, int id, CreateEpicRequest req, IMediator mediator, CancellationToken ct) =>
        {
            await mediator.Send(new UpdateEpicCommand(
                id, req.Title, req.Description, req.Priority, req.SortOrder,
                req.EstimationHours, req.EstimationPoints), ct);
            return Results.NoContent();
        })
        .WithName("UpdateEpic")
        .WithDescription("Actualiza una épica.");

        group.MapDelete("/{id:int}", async (int projectId, int id, IMediator mediator, CancellationToken ct) =>
        {
            await mediator.Send(new DeleteEpicCommand(id), ct);
            return Results.NoContent();
        })
        .WithName("DeleteEpic")
        .WithDescription("Elimina una épica y sus tareas asociadas.");

        return app;
    }
}

record CreateEpicRequest(string Title, string? Description, int Priority, int SortOrder,
    int? EstimationHours = null, int? EstimationPoints = null);
