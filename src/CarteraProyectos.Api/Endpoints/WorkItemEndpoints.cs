using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Features.WorkItems;
using MediatR;

namespace CarteraProyectos.Api.Endpoints;

public static class WorkItemEndpoints
{
    public static IEndpointRouteBuilder MapWorkItemEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/projects/{projectId:int}/workitems").RequireAuthorization();

        group.MapGet("/", async (int projectId, IMediator mediator, CancellationToken ct,
            int? epicId, string? status, int? sprintId, bool backlogOnly = false,
            int page = 1, int pageSize = 50) =>
        {
            WorkItemStatus? st = status is not null && Enum.TryParse<WorkItemStatus>(status, out var s) ? s : null;
            return Results.Ok(await mediator.Send(
                new GetWorkItemsQuery(projectId, epicId, st, page, pageSize, sprintId, backlogOnly), ct));
        })
        .WithName("GetWorkItems")
        .WithDescription("Lista las tareas de un proyecto. Filtros: epicId, status, sprintId, backlogOnly. Soporta paginación.");

        group.MapPost("/", async (int projectId, CreateWorkItemRequest req, IMediator mediator, CancellationToken ct) =>
        {
            if (!Enum.TryParse<WorkItemPriority>(req.Priority, out var priority))
                return Results.BadRequest("Prioridad no válida. Valores: Low, Medium, High, Critical.");

            var id = await mediator.Send(new CreateWorkItemCommand(
                projectId, req.Title, req.Description, priority,
                req.EpicId, req.AssigneeIds ?? [], req.SortOrder,
                req.EstimationHours, req.IsHito, req.HitoDate, req.DueDate,
                req.SprintId, req.EstimationPoints), ct);

            return Results.Created($"/api/projects/{projectId}/workitems/{id}", new { id });
        })
        .WithName("CreateWorkItem")
        .WithDescription("Crea una tarea en el proyecto.");

        group.MapPut("/{id:int}", async (int projectId, int id, CreateWorkItemRequest req, IMediator mediator, CancellationToken ct) =>
        {
            if (!Enum.TryParse<WorkItemPriority>(req.Priority, out var priority))
                return Results.BadRequest("Prioridad no válida.");

            await mediator.Send(new UpdateWorkItemCommand(
                id, req.Title, req.Description, priority,
                req.EpicId, req.AssigneeIds ?? [], req.SortOrder,
                req.EstimationHours, req.IsHito, req.HitoDate, req.DueDate,
                req.SprintId, req.EstimationPoints), ct);

            return Results.NoContent();
        })
        .WithName("UpdateWorkItem")
        .WithDescription("Actualiza los datos de una tarea.");

        group.MapDelete("/{id:int}", async (int projectId, int id, IMediator mediator, CancellationToken ct) =>
        {
            await mediator.Send(new DeleteWorkItemCommand(id), ct);
            return Results.NoContent();
        })
        .WithName("DeleteWorkItem")
        .WithDescription("Elimina una tarea.");

        group.MapPost("/{id:int}/status", async (int projectId, int id, TransitionWorkItemStatusRequest req, IMediator mediator, CancellationToken ct) =>
        {
            if (!Enum.TryParse<WorkItemStatus>(req.Status, out var newStatus))
                return Results.BadRequest("Estado no válido. Valores: Backlog, ToDo, InProgress, Blocked, Done.");

            await mediator.Send(new TransitionWorkItemStatusCommand(id, newStatus), ct);
            return Results.NoContent();
        })
        .WithName("TransitionWorkItemStatus")
        .WithDescription("Cambia el estado de una tarea. Done es terminal.");

        group.MapPost("/{id:int}/sprint", async (int projectId, int id, AssignSprintRequest req, IMediator mediator, CancellationToken ct) =>
        {
            await mediator.Send(new AssignWorkItemToSprintCommand(id, projectId, req.SprintId), ct);
            return Results.NoContent();
        })
        .WithName("AssignWorkItemToSprint")
        .WithDescription("Asigna una tarea a un sprint, o la mueve al Product Backlog (sprintId: null).");

        return app;
    }
}

record CreateWorkItemRequest(
    string Title, string? Description, string Priority,
    int? EpicId, IReadOnlyList<int>? AssigneeIds, int SortOrder,
    int? EstimationHours, bool IsHito, DateOnly? HitoDate, DateOnly? DueDate,
    int? SprintId = null, int? EstimationPoints = null);

record TransitionWorkItemStatusRequest(string Status);
record AssignSprintRequest(int? SprintId);
