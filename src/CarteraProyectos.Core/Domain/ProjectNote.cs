namespace CarteraProyectos.Core.Domain;

public class ProjectNote
{
    public int Id { get; private set; }
    public int ProjectId { get; private set; }
    public int AuthorId { get; private set; }
    public string Text { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }

    public Project? Project { get; private set; }
    public Person? Author { get; private set; }

    public static ProjectNote Create(int projectId, int authorId, string text)
        => new()
        {
            ProjectId = projectId,
            AuthorId = authorId,
            Text = text,
            CreatedAt = DateTimeOffset.UtcNow,
        };
}
