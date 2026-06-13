using CarteraProyectos.Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Interfaces;

public interface IAppDbContext
{
    DbSet<Person> Persons { get; }
    DbSet<Team> Teams { get; }
    DbSet<PersonTeamMembership> PersonTeamMemberships { get; }
    DbSet<Project> Projects { get; }
    DbSet<ProjectTeamAssignment> ProjectTeamAssignments { get; }
    DbSet<Epic> Epics { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
