namespace CarteraProyectos.Core.Domain;

public enum ProjectHealthStatus
{
    OnTrack, // EN CURSO (verde)
    AtRisk,  // EN RIESGO (amarillo)
    Blocked, // BLOQUEADO (rojo)
}

public class ProjectWeeklyUpdate
{
    public int Id { get; private set; }
    public int ProjectId { get; private set; }
    public int AuthorId { get; private set; }
    public DateOnly WeekOf { get; private set; }
    public string Summary { get; private set; } = string.Empty;
    public ProjectHealthStatus HealthStatus { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public Project? Project { get; private set; }
    public Person? Author { get; private set; }

    public static ProjectWeeklyUpdate Create(int projectId, int authorId, DateOnly weekOf, string summary, ProjectHealthStatus healthStatus)
    {
        var now = DateTimeOffset.UtcNow;
        return new()
        {
            ProjectId = projectId,
            AuthorId = authorId,
            WeekOf = weekOf,
            Summary = summary,
            HealthStatus = healthStatus,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    public void Update(string summary, ProjectHealthStatus healthStatus)
    {
        Summary = summary;
        HealthStatus = healthStatus;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
