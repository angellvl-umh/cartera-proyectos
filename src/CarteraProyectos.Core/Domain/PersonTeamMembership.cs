namespace CarteraProyectos.Core.Domain;

public class PersonTeamMembership
{
    public int PersonId { get; private set; }
    public int TeamId { get; private set; }
    public DateOnly JoinedAt { get; private set; }
    public Person? Person { get; private set; }
    public Team? Team { get; private set; }

    public static PersonTeamMembership Create(int personId, int teamId)
        => new() { PersonId = personId, TeamId = teamId, JoinedAt = DateOnly.FromDateTime(DateTime.UtcNow) };
}
