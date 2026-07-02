using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.Projects;

public static class ProjectAuthorization
{
    /// <summary>
    /// Gestor: siempre autorizado. Resto: debe pertenecer a un equipo asignado al proyecto.
    /// </summary>
    public static async Task EnsureCanManageProjectAsync(
        IAppDbContext db, int projectId, Person requester, CancellationToken ct)
    {
        if (requester.Role == PersonRole.Gestor) return;

        var isInProjectTeam = await db.ProjectTeamAssignments
            .AnyAsync(a => a.ProjectId == projectId &&
                db.PersonTeamMemberships.Any(m => m.PersonId == requester.Id && m.TeamId == a.TeamId),
                ct);

        if (!isInProjectTeam)
            throw new UnauthorizedAccessException("Debes pertenecer a un equipo asignado al proyecto.");
    }
}
