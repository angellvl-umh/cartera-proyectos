namespace CarteraProyectos.Core.Domain;

public enum WorkItemStatus
{
    Backlog,
    ToDo,
    InProgress,
    Blocked,
    Done
}

public enum WorkItemPriority
{
    Low,
    Medium,
    High,
    Critical
}

public class WorkItem
{
    public int Id { get; private set; }
    public int ProjectId { get; private set; }
    public int? EpicId { get; private set; }
    public int? SprintId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public WorkItemStatus Status { get; private set; } = WorkItemStatus.Backlog;
    public WorkItemPriority Priority { get; private set; } = WorkItemPriority.Medium;
    public int SortOrder { get; private set; }
    public int? EstimationHours { get; private set; }
    public int? EstimationPoints { get; private set; }
    public bool IsHito { get; private set; }
    public DateOnly? HitoDate { get; private set; }
    public DateOnly? DueDate { get; private set; }

    public Project? Project { get; private set; }
    public Epic? Epic { get; private set; }
    public Sprint? Sprint { get; private set; }
    public ICollection<Person> Assignees { get; private set; } = new List<Person>();
    public ICollection<Comment> Comments { get; private set; } = new List<Comment>();

    public static WorkItem Create(int projectId, string title, string? description,
        WorkItemPriority priority, int? epicId, int sortOrder,
        int? estimationHours, bool isHito, DateOnly? hitoDate, DateOnly? dueDate,
        int? sprintId = null, int? estimationPoints = null)
        => new()
        {
            ProjectId = projectId, Title = title, Description = description,
            Priority = priority, EpicId = epicId, SortOrder = sortOrder,
            EstimationHours = estimationHours, EstimationPoints = estimationPoints,
            IsHito = isHito, HitoDate = hitoDate, DueDate = dueDate, SprintId = sprintId
        };

    public void Update(string title, string? description, WorkItemPriority priority,
        int? epicId, int sortOrder, int? estimationHours, bool isHito, DateOnly? hitoDate,
        DateOnly? dueDate, int? sprintId = null, int? estimationPoints = null)
    {
        Title = title;
        Description = description;
        Priority = priority;
        EpicId = epicId;
        SortOrder = sortOrder;
        EstimationHours = estimationHours;
        EstimationPoints = estimationPoints;
        IsHito = isHito;
        HitoDate = hitoDate;
        DueDate = dueDate;
        SprintId = sprintId;
    }

    public void AssignToSprint(int? sprintId) => SprintId = sprintId;

    public void TransitionStatus(WorkItemStatus newStatus)
    {
        if (Status == WorkItemStatus.Done)
            throw new InvalidOperationException("Una tarea Done es terminal. Crea una nueva tarea para reabrirla.");

        Status = newStatus;
    }
}
