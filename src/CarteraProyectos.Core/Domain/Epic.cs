namespace CarteraProyectos.Core.Domain;

public class Epic
{
    public int Id { get; private set; }
    public int ProjectId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public int Priority { get; private set; }
    public int SortOrder { get; private set; }
    public Project? Project { get; private set; }
    // WorkItems se añadirán cuando se implemente el módulo de Épicas y Tareas
}
