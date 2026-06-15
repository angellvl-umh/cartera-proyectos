using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Features.Projects;
using CarteraProyectos.Core.Interfaces;
using MediatR;

namespace CarteraProyectos.Api.Endpoints;

public static class ProjectEndpoints
{
    public static IEndpointRouteBuilder MapProjectEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/projects").RequireAuthorization();

        group.MapGet("/", async (
            IMediator mediator, CancellationToken ct,
            string? status, int? year, int? teamId, string? complexity, string? q,
            int page = 1, int pageSize = 20) =>
        {
            ProjectStatus? st = status is not null && Enum.TryParse<ProjectStatus>(status, out var s) ? s : null;
            ProjectComplexity? cx = complexity is not null && Enum.TryParse<ProjectComplexity>(complexity, out var c) ? c : null;
            return Results.Ok(await mediator.Send(new GetProjectsQuery(st, year, teamId, cx, q, page, pageSize), ct));
        })
        .WithName("GetProjects")
        .WithDescription("Lista proyectos con filtros opcionales: status, year, teamId, complexity, q (búsqueda de texto). Soporta paginación con page y pageSize (máx 100).");

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

        group.MapPut("/{id:int}", async (int id, CreateProjectCommand body, HttpContext ctx, IAppDbContext db, IMediator mediator, CancellationToken ct) =>
        {
            var requester = await CurrentUser.ResolveAsync(ctx, db, ct);
            if (requester is null) return Results.Unauthorized();
            await mediator.Send(new UpdateProjectCommand(id, body.Title, body.Description, body.RequestingUnit,
                body.Complexity, body.PortfolioYear, body.StartDate, body.EndDate, requester.Id), ct);
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

        return app;
    }
}

record TransitionStatusRequest(string Status);
record AssignTeamRequest(int TeamId, bool IsPrimary);
