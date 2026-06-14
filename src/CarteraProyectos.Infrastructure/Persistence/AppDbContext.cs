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
    public DbSet<Sprint> Sprints => Set<Sprint>();
    public DbSet<WorkItem> WorkItems => Set<WorkItem>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<WorkItemEmbedding> WorkItemEmbeddings => Set<WorkItemEmbedding>();

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

        modelBuilder.Entity<Sprint>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.Name).IsRequired().HasMaxLength(200);
            e.Property(s => s.Goal).HasMaxLength(1000);
            e.Property(s => s.Status).HasConversion<string>();
            e.HasOne(s => s.Project).WithMany().HasForeignKey(s => s.ProjectId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WorkItem>(e =>
        {
            e.HasKey(w => w.Id);
            e.Property(w => w.Title).IsRequired().HasMaxLength(300);
            e.Property(w => w.Description).HasMaxLength(2000);
            e.Property(w => w.Status).HasConversion<string>();
            e.Property(w => w.Priority).HasConversion<string>();
            e.HasOne(w => w.Project).WithMany().HasForeignKey(w => w.ProjectId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(w => w.Epic).WithMany(ep => ep.WorkItems).HasForeignKey(w => w.EpicId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(w => w.Sprint).WithMany(s => s.WorkItems).HasForeignKey(w => w.SprintId).OnDelete(DeleteBehavior.SetNull);
            e.HasMany(w => w.Assignees).WithMany()
                .UsingEntity(j => j.ToTable("WorkItemAssignments"));
        });

        modelBuilder.Entity<Comment>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Text).IsRequired().HasMaxLength(4000);
            e.HasOne(c => c.WorkItem).WithMany(w => w.Comments).HasForeignKey(c => c.WorkItemId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(c => c.Author).WithMany().HasForeignKey(c => c.AuthorId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<WorkItemEmbedding>(e =>
        {
            e.HasKey(w => w.WorkItemId);
            e.Property(w => w.TextSnapshot).HasMaxLength(8200);
        });
    }
}
