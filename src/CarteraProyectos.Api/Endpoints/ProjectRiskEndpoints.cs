using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Features.Projects.Risks;
using CarteraProyectos.Core.Interfaces;
using MediatR;

namespace CarteraProyectos.Api.Endpoints;

public static class ProjectRiskEndpoints
{
    public static IEndpointRouteBuilder MapProjectRiskEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/projects/{projectId:int}/risks")
            .WithTags("Risks")
            .RequireAuthorization();

        group.MapGet("/", async (int projectId, IMediator mediator, CancellationToken ct,
            int page = 1, int pageSize = 20) =>
            Results.Ok(await mediator.Send(new GetProjectRisksQuery(projectId, page, pageSize), ct)))
        .WithName("GetProjectRisks")
        .WithDescription("Lista los riesgos del proyecto paginados, ordenados por estado (Open primero) y severidad descendente.");

        group.MapPost("/", async (int projectId, CreateRiskRequest req, HttpContext ctx, IAppDbContext db, IMediator mediator, CancellationToken ct) =>
        {
            if (!Enum.TryParse<RiskLevel>(req.Probability, out var probability))
                return Results.BadRequest("Probability no válido. Valores: Low, Medium, High.");
            if (!Enum.TryParse<RiskLevel>(req.Impact, out var impact))
                return Results.BadRequest("Impact no válido. Valores: Low, Medium, High.");

            var requester = await CurrentUser.ResolveAsync(ctx, db, ct);
            if (requester is null) return Results.Unauthorized();

            try
            {
                var id = await mediator.Send(new CreateProjectRiskCommand(
                    projectId, requester.Id, req.Description, probability, impact, req.MitigationPlan), ct);
                return Results.Created($"/api/projects/{projectId}/risks/{id}", new { id });
            }
            catch (KeyNotFoundException ex) { return Results.Problem(ex.Message, statusCode: 404); }
            catch (UnauthorizedAccessException ex) { return Results.Problem(ex.Message, statusCode: 403); }
        })
        .WithName("CreateProjectRisk")
        .WithDescription("Crea un nuevo riesgo en el proyecto. Solo Gestor o Jefe de equipo del proyecto. Probability e Impact: Low, Medium, High.");

        group.MapPut("/{riskId:int}", async (int projectId, int riskId, UpdateRiskRequest req, HttpContext ctx, IAppDbContext db, IMediator mediator, CancellationToken ct) =>
        {
            if (!Enum.TryParse<RiskLevel>(req.Probability, out var probability))
                return Results.BadRequest("Probability no válido. Valores: Low, Medium, High.");
            if (!Enum.TryParse<RiskLevel>(req.Impact, out var impact))
                return Results.BadRequest("Impact no válido. Valores: Low, Medium, High.");
            if (!Enum.TryParse<RiskStatus>(req.Status, out var status))
                return Results.BadRequest("Status no válido. Valores: Open, Mitigated, Closed.");

            var requester = await CurrentUser.ResolveAsync(ctx, db, ct);
            if (requester is null) return Results.Unauthorized();

            try
            {
                await mediator.Send(new UpdateProjectRiskCommand(
                    projectId, riskId, requester.Id, req.Description, probability, impact, req.MitigationPlan, status), ct);
                return Results.NoContent();
            }
            catch (KeyNotFoundException ex) { return Results.Problem(ex.Message, statusCode: 404); }
            catch (UnauthorizedAccessException ex) { return Results.Problem(ex.Message, statusCode: 403); }
        })
        .WithName("UpdateProjectRisk")
        .WithDescription("Actualiza un riesgo del proyecto. Solo Gestor o Jefe de equipo del proyecto. Status: Open, Mitigated, Closed.");

        group.MapDelete("/{riskId:int}", async (int projectId, int riskId, HttpContext ctx, IAppDbContext db, IMediator mediator, CancellationToken ct) =>
        {
            var requester = await CurrentUser.ResolveAsync(ctx, db, ct);
            if (requester is null) return Results.Unauthorized();

            try
            {
                await mediator.Send(new DeleteProjectRiskCommand(projectId, riskId, requester.Id), ct);
                return Results.NoContent();
            }
            catch (KeyNotFoundException ex) { return Results.Problem(ex.Message, statusCode: 404); }
            catch (UnauthorizedAccessException ex) { return Results.Problem(ex.Message, statusCode: 403); }
        })
        .WithName("DeleteProjectRisk")
        .WithDescription("Elimina un riesgo del proyecto. Solo Gestor o Jefe de equipo del proyecto.");

        return app;
    }
}

record CreateRiskRequest(string Description, string Probability, string Impact, string? MitigationPlan);
record UpdateRiskRequest(string Description, string Probability, string Impact, string? MitigationPlan, string Status);
