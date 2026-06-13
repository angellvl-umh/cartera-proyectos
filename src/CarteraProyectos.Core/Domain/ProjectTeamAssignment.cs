namespace CarteraProyectos.Core.Domain;

public class ProjectTeamAssignment
{
    public int ProjectId { get; private set; }
    public int TeamId { get; private set; }
    public bool IsPrimary { get; private set; }
    public Project? Project { get; private set; }
    public Team? Team { get; private set; }

    public static ProjectTeamAssignment Create(int projectId, int teamId, bool isPrimary)
        => new() { ProjectId = projectId, TeamId = teamId, IsPrimary = isPrimary };

    public void SetPrimary(bool isPrimary) => IsPrimary = isPrimary;
}
