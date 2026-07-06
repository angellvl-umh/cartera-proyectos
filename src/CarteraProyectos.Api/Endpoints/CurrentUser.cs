using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Api.Endpoints;

internal static class CurrentUser
{
    internal static async Task<Person?> ResolveAsync(HttpContext ctx, IAppDbContext db, CancellationToken ct = default)
    {
        var sub = ctx.User.FindFirst("sub")?.Value;
        if (sub is null) return null;

        var person = await db.Persons.FirstOrDefaultAsync(p => p.SubjectId == sub && p.IsActive, ct);
        if (person is not null) return person;

        // Primer login de una persona pre-registrada: las peticiones que llegan en
        // paralelo con /api/me aún no tienen el SubjectId vinculado — vincular aquí también.
        var email = ctx.User.FindFirst("email")?.Value;
        if (string.IsNullOrEmpty(email)) return null;

        person = await db.Persons.FirstOrDefaultAsync(p => p.Email == email && p.IsActive, ct);
        if (person is null) return null;

        person.UpdateSubjectId(sub);
        await db.SaveChangesAsync(ct);
        return person;
    }
}
