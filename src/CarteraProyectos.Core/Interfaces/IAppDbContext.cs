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
    DbSet<Sprint> Sprints { get; }
    DbSet<WorkItem> WorkItems { get; }
    DbSet<WorkItemStatusHistory> WorkItemStatusHistories { get; }
    DbSet<SprintStatusHistory> SprintStatusHistories { get; }
    DbSet<ProjectStatusHistory> ProjectStatusHistories { get; }
    DbSet<Comment> Comments { get; }
    DbSet<WorkItemEmbedding> WorkItemEmbeddings { get; }
    DbSet<Promoter> Promoters { get; }
    DbSet<OrganicUnit> OrganicUnits { get; }
    DbSet<Tag> Tags { get; }
    DbSet<ProjectNote> ProjectNotes { get; }
    DbSet<ProjectWeeklyUpdate> ProjectWeeklyUpdates { get; }
    DbSet<AgentActionLog> AgentActionLogs { get; }
    DbSet<ProjectRisk> ProjectRisks { get; }
    DbSet<ProjectDependency> ProjectDependencies { get; }
    DbSet<Conversation> Conversations { get; }
    DbSet<ChatMessage> ChatMessages { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
