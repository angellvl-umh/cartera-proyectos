namespace CarteraProyectos.Core.Domain;

public class AgentActionLog
{
    public int Id { get; private set; }
    public int PersonId { get; private set; }
    public string ActionName { get; private set; } = string.Empty;
    public string Payload { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }

    public static AgentActionLog Create(int personId, string actionName, string payload)
        => new()
        {
            PersonId = personId,
            ActionName = actionName,
            Payload = payload,
            CreatedAt = DateTimeOffset.UtcNow,
        };
}
