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
    public string? SubjectId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public PersonRole Role { get; private set; } = PersonRole.Desarrollador;
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Factory para crear una persona pre-registrada sin SSO vinculado.
    /// El SubjectId se asignará en el primer login a través del fallback por email.
    /// </summary>
    public static Person Create(string name, string email, PersonRole role = PersonRole.Desarrollador)
        => new() { SubjectId = null, Name = name, Email = email, Role = role, IsActive = true };

    /// <summary>
    /// Factory para crear una persona vinculada a SSO (usado en auto-provisión).
    /// </summary>
    public static Person CreateFromClaims(string subjectId, string name, string email, PersonRole role = PersonRole.Desarrollador)
        => new() { SubjectId = subjectId, Name = name, Email = email, Role = role, IsActive = true };

    public void Update(string name, string email)
    {
        Name = name;
        Email = email;
    }

    public void UpdateRole(PersonRole role) => Role = role;
    public void UpdateSubjectId(string subjectId) => SubjectId = subjectId;
    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
