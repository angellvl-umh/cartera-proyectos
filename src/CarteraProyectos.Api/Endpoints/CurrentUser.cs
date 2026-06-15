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
        return await db.Persons.FirstOrDefaultAsync(p => p.SubjectId == sub, ct);
    }
}
