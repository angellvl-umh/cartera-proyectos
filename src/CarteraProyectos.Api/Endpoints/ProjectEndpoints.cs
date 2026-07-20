using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Features.Projects;
using CarteraProyectos.Core.Features.Projects.Notes;
using CarteraProyectos.Core.Features.Projects.WeeklyUpdates;
using CarteraProyectos.Core.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CarteraProyectos.Api.Endpoints;

public static class ProjectEndpoints
{
    public static IEndpointRouteBuilder MapProjectEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/projects").RequireAuthorization();

        group.MapGet("/", async (
            IMediator mediator, CancellationToken ct,
            string? status, int? year, int? teamId, string? complexity, string? q,
            int? tagId, [FromQuery(Name = "tagIds")] int[]? tagIds, string? siptGroup, int? promoterId,
            int page = 1, int pageSize = 20) =>
        {
            ProjectStatus? st = status is not null && Enum.TryParse<ProjectStatus>(status, out var s) ? s : null;
            ProjectComplexity? cx = complexity is not null && Enum.TryParse<ProjectComplexity>(complexity, out var c) ? c : null;
            SiptGroup? sg = siptGroup is not null && Enum.TryParse<SiptGroup>(siptGroup, out var g) ? g : null;
            return Results.Ok(await mediator.Send(new GetProjectsQuery(st, year, teamId, cx, q, tagId, tagIds, sg, promoterId, page, pageSize), ct));
        })
        .WithName("GetProjects")
        .WithDescription("Lista proyectos con filtros opcionales: status, year, teamId, complexity, q, tagId, siptGroup, promoterId. Soporta paginación.");

        group.MapGet("/{id:int}", async (int id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetProjectQuery(id), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        })
        .WithName("GetProject")
        .WithDescription("Devuelve el detalle de un proyecto con sus equipos asignados.");

        group.MapPost("/", async (CreateProjectCommand cmd, HttpContext ctx, IAppDbContext db, IMediator mediator, CancellationToken ct) =>
        {
            var requester = await CurrentUser.ResolveAsync(ctx, db, ct);
            if (requester is null) return Results.Unauthorized();
            var id = await mediator.Send(cmd with { RequestingPersonId = requester.Id }, ct);
            return Results.Created($"/api/projects/{id}", new { id });
        })
        .WithName("CreateProject")
        .WithDescription("Crea un nuevo proyecto en estado Propuesto. Solo Gestor.");

        group.MapPut("/{id:int}", async (int id, UpdateProjectCommand body, HttpContext ctx, IAppDbContext db, IMediator mediator, CancellationToken ct) =>
        {
            var requester = await CurrentUser.ResolveAsync(ctx, db, ct);
            if (requester is null) return Results.Unauthorized();
            await mediator.Send(body with { Id = id, RequestingPersonId = requester.Id }, ct);
            return Results.NoContent();
        })
        .WithName("UpdateProject")
        .WithDescription("Actualiza los datos de un proyecto. Solo Gestor.");

        group.MapDelete("/{id:int}", async (int id, HttpContext ctx, IAppDbContext db, IMediator mediator, CancellationToken ct) =>
        {
            var requester = await CurrentUser.ResolveAsync(ctx, db, ct);
            if (requester is null) return Results.Unauthorized();
            await mediator.Send(new DeleteProjectCommand(id, requester.Id), ct);
            return Results.NoContent();
        })
        .WithName("DeleteProject")
        .WithDescription("Elimina un proyecto. Solo en estado Propuesto o Cancelado. Solo Gestor.");

        group.MapPost("/{id:int}/status", async (int id, TransitionStatusRequest req, HttpContext ctx, IAppDbContext db, IMediator mediator, CancellationToken ct) =>
        {
            if (!Enum.TryParse<ProjectStatus>(req.Status, out var newStatus))
                return Results.BadRequest("Estado no válido.");

            var requester = await CurrentUser.ResolveAsync(ctx, db, ct);
            if (requester is null) return Results.Unauthorized();

            await mediator.Send(new TransitionProjectStatusCommand(id, newStatus, requester.Id), ct);
            return Results.NoContent();
        })
        .WithName("TransitionProjectStatus")
        .WithDescription("Cambia el estado de un proyecto según la máquina de estados. Gestor o JefeEquipo del proyecto.");

        group.MapPost("/{id:int}/teams", async (int id, AssignTeamRequest req, HttpContext ctx, IAppDbContext db, IMediator mediator, CancellationToken ct) =>
        {
            var requester = await CurrentUser.ResolveAsync(ctx, db, ct);
            if (requester is null) return Results.Unauthorized();
            await mediator.Send(new AssignTeamToProjectCommand(id, req.TeamId, req.IsPrimary, requester.Id), ct);
            return Results.NoContent();
        })
        .WithName("AssignTeamToProject")
        .WithDescription("Asigna un equipo a un proyecto. Solo Gestor.");

        group.MapDelete("/{id:int}/teams/{teamId:int}", async (int id, int teamId, HttpContext ctx, IAppDbContext db, IMediator mediator, CancellationToken ct) =>
        {
            var requester = await CurrentUser.ResolveAsync(ctx, db, ct);
            if (requester is null) return Results.Unauthorized();
            await mediator.Send(new RemoveTeamFromProjectCommand(id, teamId, requester.Id), ct);
            return Results.NoContent();
        })
        .WithName("RemoveTeamFromProject")
        .WithDescription("Desasigna un equipo de un proyecto. Solo Gestor.");

        group.MapGet("/{id:int}/notes", async (int id, IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new GetProjectNotesQuery(id), ct)))
            .WithName("GetProjectNotes")
            .WithDescription("Lista todas las notas de un proyecto ordenadas por fecha.");

        group.MapPost("/{id:int}/notes", async (int id, CreateNoteRequest req, HttpContext ctx, IAppDbContext db, IMediator mediator, CancellationToken ct) =>
        {
            var requester = await CurrentUser.ResolveAsync(ctx, db, ct);
            if (requester is null) return Results.Unauthorized();
            var noteId = await mediator.Send(new CreateProjectNoteCommand(id, requester.Id, req.Text), ct);
            return Results.Created($"/api/projects/{id}/notes/{noteId}", new { id = noteId });
        })
        .WithName("CreateProjectNote")
        .WithDescription("Añade una nota al proyecto. Gestor y JefeEquipo del proyecto.");

        group.MapDelete("/{id:int}/notes/{noteId:int}", async (int id, int noteId, HttpContext ctx, IAppDbContext db, IMediator mediator, CancellationToken ct) =>
        {
            var requester = await CurrentUser.ResolveAsync(ctx, db, ct);
            if (requester is null) return Results.Unauthorized();
            await mediator.Send(new DeleteProjectNoteCommand(id, noteId, requester.Id), ct);
            return Results.NoContent();
        })
        .WithName("DeleteProjectNote")
        .WithDescription("Elimina una nota. Solo el autor o el Gestor.");

        group.MapGet("/{id:int}/weekly-updates", async (int id, IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new GetProjectWeeklyUpdatesQuery(id), ct)))
            .WithName("GetProjectWeeklyUpdates")
            .WithDescription("Lista todas las actualizaciones semanales de un proyecto ordenadas por semana descendente.");

        group.MapPost("/{id:int}/weekly-updates", async (int id, UpsertWeeklyUpdateRequest req, HttpContext ctx, IAppDbContext db, IMediator mediator, CancellationToken ct) =>
        {
            var requester = await CurrentUser.ResolveAsync(ctx, db, ct);
            if (requester is null) return Results.Unauthorized();
            var updateId = await mediator.Send(new UpsertProjectWeeklyUpdateCommand(id, requester.Id, req.Summary, req.HealthStatus), ct);
            return Results.Ok(new { id = updateId });
        })
        .WithName("UpsertProjectWeeklyUpdate")
        .WithDescription("Crea o actualiza la actualización semanal del proyecto para la semana actual. Gestor, JefeEquipo del proyecto o Desarrollador en equipo asignado.");

        group.MapGet("/{id:int}/status-history", async (int id, IMediator mediator, CancellationToken ct) =>
        {
            try { return Results.Ok(await mediator.Send(new GetProjectStatusHistoryQuery(id), ct)); }
            catch (KeyNotFoundException) { return Results.NotFound(); }
        })
        .WithName("GetProjectStatusHistory")
        .WithDescription("Devuelve el histórico de cambios de estado del proyecto, ordenado cronológicamente.");

        return app;
    }
}

record TransitionStatusRequest(string Status);
record AssignTeamRequest(int TeamId, bool IsPrimary);
record CreateNoteRequest(string Text);
record UpsertWeeklyUpdateRequest(string Summary, ProjectHealthStatus HealthStatus);
