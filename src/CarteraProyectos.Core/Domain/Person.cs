namespace CarteraProyectos.Core.Domain;

public enum PersonRole
{
    Desarrollador,
    JefeEquipo,
    Gestor
}

public class Person
{
    public int Id { get; private set; }
    public string SubjectId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public PersonRole Role { get; private set; } = PersonRole.Desarrollador;

    public static Person CreateFromClaims(string subjectId, string name, string email, PersonRole role = PersonRole.Desarrollador)
        => new() { SubjectId = subjectId, Name = name, Email = email, Role = role };

    public void UpdateRole(PersonRole role) => Role = role;
    public void UpdateSubjectId(string subjectId) => SubjectId = subjectId;
}
