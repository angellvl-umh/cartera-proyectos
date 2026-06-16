namespace CarteraProyectos.Core.Domain;

public class Tag
{
    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Color { get; private set; }

    public static Tag Create(string name, string? color = null)
        => new() { Name = name, Color = color };

    public void Update(string name, string? color)
    {
        Name = name;
        Color = color;
    }
}
