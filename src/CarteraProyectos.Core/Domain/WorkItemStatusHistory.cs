namespace CarteraProyectos.Core.Domain;

public class WorkItemStatusHistory
{
    public int Id { get; private set; }
    public int WorkItemId { get; private set; }
    public WorkItemStatus? FromStatus { get; private set; }
    public WorkItemStatus ToStatus { get; private set; }
    public int ChangedById { get; private set; }
    public DateTime ChangedAt { get; private set; }

    public WorkItem? WorkItem { get; private set; }
    public Person? ChangedBy { get; private set; }

    public static WorkItemStatusHistory Create(WorkItem workItem, WorkItemStatus? fromStatus,
        WorkItemStatus toStatus, int changedById, DateTime? changedAt = null)
        => new()
        {
            WorkItem = workItem,
            FromStatus = fromStatus,
            ToStatus = toStatus,
            ChangedById = changedById,
            ChangedAt = changedAt ?? DateTime.UtcNow
        };
}
