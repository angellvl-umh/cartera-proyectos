namespace CarteraProyectos.Core.Domain;

public class Epic
{
    public int Id { get; private set; }
    public int ProjectId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public int Priority { get; private set; }
    public int SortOrder { get; private set; }
    public int? EstimationHours { get; private set; }
    public int? EstimationPoints { get; private set; }
    public Project? Project { get; private set; }
    public ICollection<WorkItem> WorkItems { get; private set; } = new List<WorkItem>();

    public static Epic Create(int projectId, string title, string? description, int priority, int sortOrder,
        int? estimationHours = null, int? estimationPoints = null)
        => new()
        {
            ProjectId = projectId, Title = title, Description = description,
            Priority = priority, SortOrder = sortOrder,
            EstimationHours = estimationHours, EstimationPoints = estimationPoints
        };

    public void Update(string title, string? description, int priority, int sortOrder,
        int? estimationHours = null, int? estimationPoints = null)
    {
        Title = title;
        Description = description;
        Priority = priority;
        SortOrder = sortOrder;
        EstimationHours = estimationHours;
        EstimationPoints = estimationPoints;
    }
}
