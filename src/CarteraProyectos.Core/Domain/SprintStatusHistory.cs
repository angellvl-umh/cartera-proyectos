namespace CarteraProyectos.Core.Domain;

public class SprintStatusHistory
{
    public int Id { get; private set; }
    public int SprintId { get; private set; }
    public SprintStatus? FromStatus { get; private set; }
    public SprintStatus ToStatus { get; private set; }
    public int ChangedById { get; private set; }
    public DateTime ChangedAt { get; private set; }

    public Sprint? Sprint { get; private set; }
    public Person? ChangedBy { get; private set; }

    public static SprintStatusHistory Create(Sprint sprint, SprintStatus? fromStatus,
        SprintStatus toStatus, int changedById)
        => new()
        {
            Sprint = sprint,
            FromStatus = fromStatus,
            ToStatus = toStatus,
            ChangedById = changedById,
            ChangedAt = DateTime.UtcNow
        };
}
