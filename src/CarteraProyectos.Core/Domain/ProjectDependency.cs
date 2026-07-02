namespace CarteraProyectos.Core.Domain;

public class ProjectDependency
{
    public int Id { get; private set; }
    public int ProjectId { get; private set; }
    public int DependsOnProjectId { get; private set; }
    public string? Description { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public Project? Project { get; private set; }
    public Project? DependsOnProject { get; private set; }

    public static ProjectDependency Create(int projectId, int dependsOnProjectId, string? description)
        => new()
        {
            ProjectId = projectId,
            DependsOnProjectId = dependsOnProjectId,
            Description = description,
            CreatedAt = DateTimeOffset.UtcNow,
        };
}
