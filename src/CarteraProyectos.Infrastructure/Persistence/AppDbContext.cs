using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IAppDbContext
{
    public DbSet<Person> Persons => Set<Person>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<PersonTeamMembership> PersonTeamMemberships => Set<PersonTeamMembership>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectTeamAssignment> ProjectTeamAssignments => Set<ProjectTeamAssignment>();
    public DbSet<Epic> Epics => Set<Epic>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Person>(e =>
        {
            e.HasKey(p => p.Id);
            e.HasIndex(p => p.SubjectId).IsUnique();
            e.HasIndex(p => p.Email).IsUnique();
            e.Property(p => p.Role).HasConversion<string>();
        });

        modelBuilder.Entity<Team>(e =>
        {
            e.HasKey(t => t.Id);
            e.HasIndex(t => t.Name).IsUnique();
            e.Property(t => t.Name).IsRequired().HasMaxLength(200);
            e.Property(t => t.Description).HasMaxLength(1000);
            e.HasOne(t => t.Lead)
                .WithMany()
                .HasForeignKey(t => t.LeadPersonId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<PersonTeamMembership>(e =>
        {
            e.HasKey(m => new { m.PersonId, m.TeamId });
            e.HasOne(m => m.Person).WithMany().HasForeignKey(m => m.PersonId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(m => m.Team).WithMany(t => t.Members).HasForeignKey(m => m.TeamId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Project>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Title).IsRequired().HasMaxLength(300);
            e.Property(p => p.Description).HasMaxLength(2000);
            e.Property(p => p.RequestingUnit).IsRequired().HasMaxLength(200);
            e.Property(p => p.Status).HasConversion<string>();
            e.Property(p => p.Complexity).HasConversion<string>();
        });

        modelBuilder.Entity<ProjectTeamAssignment>(e =>
        {
            e.HasKey(a => new { a.ProjectId, a.TeamId });
            e.HasOne(a => a.Project).WithMany(p => p.Teams).HasForeignKey(a => a.ProjectId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(a => a.Team).WithMany(t => t.Projects).HasForeignKey(a => a.TeamId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Epic>(e =>
        {
            e.HasKey(ep => ep.Id);
            e.Property(ep => ep.Title).IsRequired().HasMaxLength(300);
            e.Property(ep => ep.Description).HasMaxLength(2000);
            e.HasOne(ep => ep.Project).WithMany(p => p.Epics).HasForeignKey(ep => ep.ProjectId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
