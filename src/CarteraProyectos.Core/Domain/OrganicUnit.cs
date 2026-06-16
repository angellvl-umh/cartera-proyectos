namespace CarteraProyectos.Core.Domain;

public class OrganicUnit
{
    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Code { get; private set; }

    public static OrganicUnit Create(string name, string? code = null)
        => new() { Name = name, Code = code };

    public void Update(string name, string? code)
    {
        Name = name;
        Code = code;
    }
}
