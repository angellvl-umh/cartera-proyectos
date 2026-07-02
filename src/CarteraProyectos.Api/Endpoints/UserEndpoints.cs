using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Api.Endpoints;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/me", async (HttpContext ctx, IAppDbContext db, IConfiguration configuration) =>
        {
            var sub = ctx.User.FindFirst("sub")?.Value;
            if (sub is null) return Results.Unauthorized();

            var person = await db.Persons.FirstOrDefaultAsync(p => p.SubjectId == sub);
            if (person is null)
            {
                var name = ctx.User.FindFirst("name")?.Value
                        ?? ctx.User.FindFirst("preferred_username")?.Value
                        ?? "Unknown";
                var email = ctx.User.FindFirst("email")?.Value ?? "";

                // Fallback: buscar por email (el sub pudo cambiar si se recreó el realm)
                person = await db.Persons.FirstOrDefaultAsync(p => p.Email == email);
                if (person is not null)
                {
                    person.UpdateSubjectId(sub);
                    await db.SaveChangesAsync();
                }
                else
                {
                    var initialGestorEmails = configuration
                        .GetSection("Admin:InitialGestorEmails")
                        .Get<string[]>() ?? [];

                    var role = initialGestorEmails.Contains(email, StringComparer.OrdinalIgnoreCase)
                        ? PersonRole.Gestor
                        : PersonRole.Desarrollador;

                    person = Person.CreateFromClaims(sub, name, email, role);
                    db.Persons.Add(person);
                    await db.SaveChangesAsync();
                }
            }

            return Results.Ok(new
            {
                person.Id,
                person.SubjectId,
                person.Name,
                person.Email,
                person.IsActive,
                Role = person.Role.ToString()
            });
        })
        .RequireAuthorization()
        .WithName("GetCurrentUser")
        .WithDescription("Devuelve la información del usuario autenticado. Crea el usuario si no existe.");

        return app;
    }
}
