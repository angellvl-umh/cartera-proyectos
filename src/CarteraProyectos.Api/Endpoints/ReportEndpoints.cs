using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Features.Persons;
using CarteraProyectos.Core.Features.Reports;
using CarteraProyectos.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Api.Endpoints;

public static class ReportEndpoints
{
    public static IEndpointRouteBuilder MapReportEndpoints(this IEndpointRouteBuilder app)
    {
        // Informe de avance de un proyecto
        app.MapGet("/api/projects/{projectId:int}/report",
            async (int projectId, IMediator mediator, CancellationToken ct) =>
            {
                try { return Results.Ok(await mediator.Send(new GetProjectReportQuery(projectId), ct)); }
                catch (KeyNotFoundException) { return Results.NotFound(); }
            })
            .RequireAuthorization()
            .WithName("GetProjectReport")
            .WithDescription("Informe de avance del proyecto: épicas con % completado, hitos y resumen de sprints.");

        // Mis tareas (cross-proyecto)
        app.MapGet("/api/me/workitems",
            async (HttpContext ctx, IAppDbContext db, IMediator mediator, CancellationToken ct,
                   string? status = null, int page = 1, int pageSize = 50) =>
            {
                var sub = ctx.User.FindFirst("sub")?.Value;
                if (sub is null) return Results.Unauthorized();

                var person = await db.Persons.FirstOrDefaultAsync(p => p.SubjectId == sub, ct);
                if (person is null) return Results.Unauthorized();

                WorkItemStatus? statusEnum = null;
                if (status is not null && Enum.TryParse<WorkItemStatus>(status, out var parsed))
                    statusEnum = parsed;

                return Results.Ok(await mediator.Send(
                    new GetMyWorkItemsQuery(person.Id, statusEnum, page, pageSize), ct));
            })
            .RequireAuthorization()
            .WithName("GetMyWorkItems")
            .WithDescription("Lista todas las tareas asignadas al usuario autenticado, de todos sus proyectos.");

        // Vista de cartera (portfolio)
        app.MapGet("/api/portfolio",
            async (IMediator mediator, CancellationToken ct, int? year = null, string? status = null) =>
                Results.Ok(await mediator.Send(new GetPortfolioQuery(year, status), ct)))
            .RequireAuthorization()
            .WithName("GetPortfolio")
            .WithDescription("Vista de cartera: todos los proyectos con progreso, equipos y hitos. Filtrable por año y estado.");

        // Perfil de persona
        app.MapGet("/api/persons/{id:int}/profile",
            async (int id, IMediator mediator, CancellationToken ct) =>
            {
                try { return Results.Ok(await mediator.Send(new GetPersonProfileQuery(id), ct)); }
                catch (KeyNotFoundException) { return Results.NotFound(); }
            })
            .RequireAuthorization()
            .WithName("GetPersonProfile")
            .WithDescription("Perfil de una persona: equipos, carga de trabajo y tareas activas.");

        // Capacidad de equipos
        app.MapGet("/api/capacity",
            async (IMediator mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetCapacityQuery(), ct)))
            .RequireAuthorization()
            .WithName("GetCapacity")
            .WithDescription("Carga de trabajo actual de todos los equipos con nivel de carga por persona (Green/Yellow/Red).");

        // Informe semanal de seguimiento de cartera
        app.MapGet("/api/reports/weekly-portfolio",
            async (IMediator mediator, CancellationToken ct, int? year = null, int? teamId = null, string? siptGroup = null) =>
                Results.Ok(await mediator.Send(new GetWeeklyPortfolioReportQuery(year, teamId, siptGroup), ct)))
            .RequireAuthorization()
            .WithName("GetWeeklyPortfolioReport")
            .WithDescription("Informe semanal de seguimiento de cartera: proyectos en riesgo y otros clasificados por estado de actualización de esta semana. Filtrable por año, equipo y grupo SIPT.");

        return app;
    }
}
