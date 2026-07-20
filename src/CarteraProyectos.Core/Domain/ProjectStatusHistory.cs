namespace CarteraProyectos.Core.Domain;

public class ProjectStatusHistory
{
    public int Id { get; private set; }
    public int ProjectId { get; private set; }
    public ProjectStatus? FromStatus { get; private set; }
    public ProjectStatus ToStatus { get; private set; }
    public int ChangedById { get; private set; }
    public DateTime ChangedAt { get; private set; }

    public Project? Project { get; private set; }
    public Person? ChangedBy { get; private set; }

    public static ProjectStatusHistory Create(Project project, ProjectStatus? fromStatus,
        ProjectStatus toStatus, int changedById)
        => new()
        {
            Project = project,
            FromStatus = fromStatus,
            ToStatus = toStatus,
            ChangedById = changedById,
            ChangedAt = DateTime.UtcNow
        };
}
