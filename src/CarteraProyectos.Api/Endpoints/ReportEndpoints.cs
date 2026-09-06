using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Features.Activity;
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
                var person = await CurrentUser.ResolveAsync(ctx, db, ct);
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
            async (IMediator mediator, CancellationToken ct, int? year = null, int? teamId = null) =>
                Results.Ok(await mediator.Send(new GetWeeklyPortfolioReportQuery(year, teamId), ct)))
            .RequireAuthorization()
            .WithName("GetWeeklyPortfolioReport")
            .WithDescription("Informe semanal de seguimiento de cartera: proyectos en riesgo y otros clasificados por estado de actualización de esta semana. Filtrable por año y equipo.");

        // Velocity por proyecto
        app.MapGet("/api/projects/{projectId:int}/velocity",
            async (int projectId, IMediator mediator, CancellationToken ct) =>
            {
                try { return Results.Ok(await mediator.Send(new GetProjectVelocityQuery(projectId), ct)); }
                catch (KeyNotFoundException) { return Results.NotFound(); }
            })
            .RequireAuthorization()
            .WithName("GetProjectVelocity")
            .WithDescription("Velocidad del equipo por proyecto: puntos comprometidos y entregados por sprint completado, con la media de velocidad.");

        // Cycle time / lead time por proyecto
        app.MapGet("/api/projects/{projectId:int}/cycle-time",
            async (int projectId, IMediator mediator, CancellationToken ct) =>
            {
                try { return Results.Ok(await mediator.Send(new GetProjectCycleTimeQuery(projectId), ct)); }
                catch (KeyNotFoundException) { return Results.NotFound(); }
            })
            .RequireAuthorization()
            .WithName("GetProjectCycleTime")
            .WithDescription("Métricas de cycle time y lead time por proyecto: tiempo desde inicio de trabajo hasta Done (cycle time) y desde creación hasta Done (lead time), para las tareas completadas.");

        // Roadmap de cartera
        app.MapGet("/api/portfolio/roadmap",
            async (IMediator mediator, CancellationToken ct, int? year = null) =>
                Results.Ok(await mediator.Send(new GetPortfolioRoadmapQuery(year), ct)))
            .RequireAuthorization()
            .WithName("GetPortfolioRoadmap")
            .WithDescription(
                "Roadmap visual de la cartera agrupado por equipo primario. " +
                "Devuelve los proyectos con sus fechas y hitos para el año indicado (por defecto el año actual). " +
                "Los proyectos sin StartDate aparecen en 'undated'; los sin equipo asignado en 'unassigned'.");

        // Capacidad prospectiva trimestral
        app.MapGet("/api/capacity/forecast",
            async (IMediator mediator, CancellationToken ct, int? year = null) =>
                Results.Ok(await mediator.Send(new GetCapacityForecastQuery(year), ct)))
            .RequireAuthorization()
            .WithName("GetCapacityForecast")
            .WithDescription(
                "Previsión de carga de trabajo por equipo y trimestre para el año indicado (por defecto el año actual). " +
                "La demanda se estima a partir de la complejidad y las fechas de los proyectos activos. " +
                "Nivel de carga: Green <70 %, Yellow 70–100 %, Red >100 %.");

        // Feed de actividad reciente
        app.MapGet("/api/activity",
            async (IMediator mediator, CancellationToken ct,
                   int? projectId = null, int? teamId = null, int? personId = null,
                   int page = 1, int pageSize = 20) =>
                Results.Ok(await mediator.Send(
                    new GetActivityFeedQuery(projectId, teamId, personId, page, pageSize), ct)))
            .RequireAuthorization()
            .WithName("GetActivityFeed")
            .WithDescription(
                "Feed de actividad reciente de la plataforma en orden cronológico inverso: " +
                "cambios de estado de proyecto, tareas creadas, tareas completadas, comentarios y " +
                "actualizaciones semanales de avance. Filtrable por proyecto, equipo y persona (autor/actor). " +
                "Paginado (pageSize máximo 100).");

        return app;
    }
}
