using CarteraProyectos.Core.Features.Users;
using MediatR;

namespace CarteraProyectos.Api.Endpoints;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/me", async (HttpContext ctx, ISender sender, IConfiguration configuration) =>
        {
            var sub = ctx.User.FindFirst("sub")?.Value;
            if (sub is null) return Results.Unauthorized();

            var name  = ctx.User.FindFirst("name")?.Value
                     ?? ctx.User.FindFirst("preferred_username")?.Value
                     ?? "Unknown";
            var email = ctx.User.FindFirst("email")?.Value ?? "";

            var bootstrapEmails = configuration
                .GetSection("Admin:InitialGestorEmails")
                .Get<string[]>() ?? [];

            var result = await sender.Send(
                new ResolveCurrentUserCommand(sub, name, email, bootstrapEmails));

            return result.Status switch
            {
                ResolveUserStatus.Ok => Results.Ok(new
                {
                    result.Id,
                    result.SubjectId,
                    result.Name,
                    result.Email,
                    result.IsActive,
                    Role = result.Role
                }),
                ResolveUserStatus.Inactive => Results.Problem(
                    "Tu usuario está desactivado. Contacta con un gestor de la cartera.",
                    statusCode: 403),
                _ => Results.Problem(
                    "No tienes acceso a la aplicación. Solicita el alta a un gestor de la cartera.",
                    statusCode: 403),
            };
        })
        .RequireAuthorization()
        .WithName("GetCurrentUser")
        .WithDescription(
            "Devuelve la información del usuario autenticado. " +
            "Vincula automáticamente una Person pre-registrada por email si aún no tiene SubjectId. " +
            "Devuelve 403 si el usuario autenticado no existe como Person o está desactivado. " +
            "Solo los emails de Admin:InitialGestorEmails se auto-crean como Gestor en el primer login (bootstrap).");

        return app;
    }
}
