namespace CarteraProyectos.Core.Domain;

public class WorkItemEmbedding
{
    public int WorkItemId { get; private set; }
    public float[] Embedding { get; private set; } = [];
    public string TextSnapshot { get; private set; } = string.Empty;
    public DateTime IndexedAt { get; private set; }

    public static WorkItemEmbedding Create(int workItemId, float[] embedding, string textSnapshot)
        => new()
        {
            WorkItemId = workItemId,
            Embedding = embedding,
            TextSnapshot = textSnapshot,
            IndexedAt = DateTime.UtcNow,
        };

    public void Update(float[] embedding, string textSnapshot)
    {
        Embedding = embedding;
        TextSnapshot = textSnapshot;
        IndexedAt = DateTime.UtcNow;
    }
}
