namespace CarteraProyectos.Core.Domain;

public class Comment
{
    public int Id { get; private set; }
    public int WorkItemId { get; private set; }
    public int AuthorId { get; private set; }
    public string Text { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    public WorkItem? WorkItem { get; private set; }
    public Person? Author { get; private set; }

    public static Comment Create(int workItemId, int authorId, string text)
        => new() { WorkItemId = workItemId, AuthorId = authorId, Text = text, CreatedAt = DateTime.UtcNow };
}
