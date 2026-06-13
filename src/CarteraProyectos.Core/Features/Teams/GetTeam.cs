using CarteraProyectos.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.Teams;

public record GetTeamQuery(int Id) : IRequest<TeamDetailDto?>;

public record TeamMemberDto(int Id, string Name, string Email, string Role, DateOnly JoinedAt);

public record TeamDetailDto(
    int Id,
    string Name,
    string? Description,
    int? LeadPersonId,
    string? LeadName,
    List<TeamMemberDto> Members);

public sealed class GetTeamHandler(IAppDbContext db) : IRequestHandler<GetTeamQuery, TeamDetailDto?>
{
    public async Task<TeamDetailDto?> Handle(GetTeamQuery request, CancellationToken cancellationToken)
    {
        var team = await db.Teams
            .Include(t => t.Lead)
            .Include(t => t.Members).ThenInclude(m => m.Person)
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

        if (team is null) return null;

        return new TeamDetailDto(
            team.Id,
            team.Name,
            team.Description,
            team.LeadPersonId,
            team.Lead?.Name,
            team.Members.Select(m => new TeamMemberDto(
                m.PersonId,
                m.Person!.Name,
                m.Person!.Email,
                m.Person!.Role.ToString(),
                m.JoinedAt)).ToList());
    }
}
