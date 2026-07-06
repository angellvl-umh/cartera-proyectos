using CarteraProyectos.Core.Features.Dashboard;
using CarteraProyectos.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Api.Endpoints;

public static class DashboardEndpoints
{
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/dashboard", async (HttpContext ctx, IAppDbContext db, IMediator mediator, CancellationToken ct) =>
        {
            var person = await CurrentUser.ResolveAsync(ctx, db, ct);
            if (person is null) return Results.Unauthorized();

            return Results.Ok(await mediator.Send(new GetDashboardQuery(person.Id), ct));
        })
        .RequireAuthorization()
        .WithName("GetDashboard")
        .WithDescription("Panel de control del usuario autenticado: proyectos, sprints activos y estadísticas de tareas.");

        return app;
    }
}
