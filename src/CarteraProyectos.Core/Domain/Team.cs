namespace CarteraProyectos.Core.Domain;

public class Team
{
    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public int? LeadPersonId { get; private set; }
    public Person? Lead { get; private set; }
    public ICollection<PersonTeamMembership> Members { get; } = [];
    public ICollection<ProjectTeamAssignment> Projects { get; } = [];

    public static Team Create(string name, string? description, int? leadPersonId)
        => new() { Name = name, Description = description, LeadPersonId = leadPersonId };

    public void Update(string name, string? description, int? leadPersonId)
    {
        Name = name;
        Description = description;
        LeadPersonId = leadPersonId;
    }
}
