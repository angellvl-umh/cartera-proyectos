namespace CarteraProyectos.Core.Domain;

public enum SprintStatus { Planning, Active, Completed }

public class Sprint
{
    private static readonly Dictionary<SprintStatus, SprintStatus[]> AllowedTransitions = new()
    {
        [SprintStatus.Planning]   = [SprintStatus.Active],
        [SprintStatus.Active]     = [SprintStatus.Completed],
        [SprintStatus.Completed]  = [],
    };

    public int Id { get; private set; }
    public int ProjectId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Goal { get; private set; }
    public DateOnly? StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public SprintStatus Status { get; private set; } = SprintStatus.Planning;
    public int? Capacity { get; private set; }

    public Project? Project { get; private set; }
    public ICollection<WorkItem> WorkItems { get; private set; } = new List<WorkItem>();

    private Sprint() { }

    public static Sprint Create(int projectId, string name, string? goal,
        DateOnly? startDate, DateOnly? endDate, int? capacity)
        => new()
        {
            ProjectId = projectId, Name = name, Goal = goal,
            StartDate = startDate, EndDate = endDate, Capacity = capacity,
        };

    public void Update(string name, string? goal, DateOnly? startDate, DateOnly? endDate, int? capacity)
    {
        if (Status != SprintStatus.Planning)
            throw new InvalidOperationException("Solo se puede editar un sprint en estado Planning.");
        Name = name;
        Goal = goal;
        StartDate = startDate;
        EndDate = endDate;
        Capacity = capacity;
    }

    public void TransitionStatus(SprintStatus newStatus)
    {
        if (!AllowedTransitions[Status].Contains(newStatus))
            throw new InvalidOperationException($"No se puede transicionar de {Status} a {newStatus}.");
        Status = newStatus;
    }
}
