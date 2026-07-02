using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.Teams;

public record CreateTeamCommand(string Name, string? Description, int? LeadPersonId, int RequestingPersonId = 0) : IRequest<int>;

public sealed class CreateTeamValidator : AbstractValidator<CreateTeamCommand>
{
    public CreateTeamValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000).When(x => x.Description is not null);
    }
}

public sealed class CreateTeamHandler(IAppDbContext db) : IRequestHandler<CreateTeamCommand, int>
{
    public async Task<int> Handle(CreateTeamCommand request, CancellationToken cancellationToken)
    {
        var requester = await db.Persons.FindAsync([request.RequestingPersonId], cancellationToken)
            ?? throw new KeyNotFoundException($"Persona con Id {request.RequestingPersonId} no encontrada.");
        if (requester.Role != PersonRole.Gestor)
            throw new UnauthorizedAccessException("Solo el Gestor puede crear equipos.");

        if (await db.Teams.AnyAsync(t => t.Name == request.Name, cancellationToken))
            throw new InvalidOperationException($"Ya existe un equipo con el nombre '{request.Name}'.");

        if (request.LeadPersonId.HasValue)
        {
            var lead = await db.Persons.FindAsync([request.LeadPersonId.Value], cancellationToken);
            if (lead is null)
                throw new KeyNotFoundException($"Persona con Id {request.LeadPersonId} no encontrada.");
        }

        var team = Team.Create(request.Name, request.Description, request.LeadPersonId);
        db.Teams.Add(team);
        await db.SaveChangesAsync(cancellationToken);
        return team.Id;
    }
}
