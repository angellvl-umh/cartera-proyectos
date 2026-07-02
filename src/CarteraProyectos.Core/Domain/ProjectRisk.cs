namespace CarteraProyectos.Core.Domain;

public enum RiskLevel { Low, Medium, High }
public enum RiskStatus { Open, Mitigated, Closed }

public class ProjectRisk
{
    public int Id { get; private set; }
    public int ProjectId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public RiskLevel Probability { get; private set; }
    public RiskLevel Impact { get; private set; }
    public string? MitigationPlan { get; private set; }
    public RiskStatus Status { get; private set; } = RiskStatus.Open;
    public int CreatedById { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public Project? Project { get; private set; }
    public Person? CreatedBy { get; private set; }

    /// <summary>
    /// Severidad calculada: (probabilidad+1) * (impacto+1). Rango 1–9.
    /// No mapeada a BD.
    /// </summary>
    public int Severity => ((int)Probability + 1) * ((int)Impact + 1);

    public static ProjectRisk Create(
        int projectId, string description,
        RiskLevel probability, RiskLevel impact,
        string? mitigationPlan, int createdById)
    {
        var now = DateTimeOffset.UtcNow;
        return new ProjectRisk
        {
            ProjectId = projectId,
            Description = description,
            Probability = probability,
            Impact = impact,
            MitigationPlan = mitigationPlan,
            Status = RiskStatus.Open,
            CreatedById = createdById,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    public void Update(string description, RiskLevel probability, RiskLevel impact,
        string? mitigationPlan, RiskStatus status)
    {
        Description = description;
        Probability = probability;
        Impact = impact;
        MitigationPlan = mitigationPlan;
        Status = status;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
