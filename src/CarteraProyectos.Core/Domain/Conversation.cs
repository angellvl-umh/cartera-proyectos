namespace CarteraProyectos.Core.Domain;

public class Conversation
{
    public int Id { get; private set; }
    public int PersonId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public Person? Person { get; private set; }
    public ICollection<ChatMessage> Messages { get; private set; } = new List<ChatMessage>();

    private Conversation() { }

    public static Conversation Create(int personId, string title)
    {
        var now = DateTimeOffset.UtcNow;
        return new Conversation
        {
            PersonId  = personId,
            Title     = title,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    public void Touch() => UpdatedAt = DateTimeOffset.UtcNow;

    public void UpdateTitle(string title)
    {
        Title     = title;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
