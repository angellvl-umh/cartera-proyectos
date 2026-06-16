namespace CarteraProyectos.Core.Domain;

public class Promoter
{
    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;

    public static Promoter Create(string name) => new() { Name = name };

    public void Update(string name) => Name = name;
}
