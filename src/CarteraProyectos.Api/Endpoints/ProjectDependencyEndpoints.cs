using CarteraProyectos.Core.Features.Projects.Dependencies;
using CarteraProyectos.Core.Interfaces;
using MediatR;

namespace CarteraProyectos.Api.Endpoints;

public static class ProjectDependencyEndpoints
{
    public static IEndpointRouteBuilder MapProjectDependencyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/projects/{projectId:int}/dependencies")
            .WithTags("Dependencies")
            .RequireAuthorization();

        group.MapGet("/", async (int projectId, IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new GetProjectDependenciesQuery(projectId), ct)))
        .WithName("GetProjectDependencies")
        .WithDescription("Devuelve las dependencias del proyecto: proyectos de los que depende (dependsOn) y proyectos que dependen de éste (dependents).");

        group.MapPost("/", async (int projectId, CreateDependencyRequest req, HttpContext ctx, IAppDbContext db, IMediator mediator, CancellationToken ct) =>
        {
            var requester = await CurrentUser.ResolveAsync(ctx, db, ct);
            if (requester is null) return Results.Unauthorized();

            try
            {
                var id = await mediator.Send(new CreateProjectDependencyCommand(
                    projectId, req.DependsOnProjectId, req.Description, requester.Id), ct);
                return Results.Created($"/api/projects/{projectId}/dependencies/{id}", new { id });
            }
            catch (KeyNotFoundException ex) { return Results.Problem(ex.Message, statusCode: 404); }
            catch (UnauthorizedAccessException ex) { return Results.Problem(ex.Message, statusCode: 403); }
            catch (InvalidOperationException ex) { return Results.BadRequest(ex.Message); }
        })
        .WithName("CreateProjectDependency")
        .WithDescription("Crea una dependencia entre proyectos. El proyecto actual dependerá del proyecto indicado en dependsOnProjectId. Solo Gestor o Jefe de equipo del proyecto.");

        group.MapDelete("/{dependencyId:int}", async (int projectId, int dependencyId, HttpContext ctx, IAppDbContext db, IMediator mediator, CancellationToken ct) =>
        {
            var requester = await CurrentUser.ResolveAsync(ctx, db, ct);
            if (requester is null) return Results.Unauthorized();

            try
            {
                await mediator.Send(new DeleteProjectDependencyCommand(projectId, dependencyId, requester.Id), ct);
                return Results.NoContent();
            }
            catch (KeyNotFoundException ex) { return Results.Problem(ex.Message, statusCode: 404); }
            catch (UnauthorizedAccessException ex) { return Results.Problem(ex.Message, statusCode: 403); }
        })
        .WithName("DeleteProjectDependency")
        .WithDescription("Elimina una dependencia entre proyectos. Solo Gestor o Jefe de equipo del proyecto.");

        return app;
    }
}

record CreateDependencyRequest(int DependsOnProjectId, string? Description);
