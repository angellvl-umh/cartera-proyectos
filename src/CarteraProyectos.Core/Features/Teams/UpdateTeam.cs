using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.Teams;

public record UpdateTeamCommand(int Id, string Name, string? Description, int? LeadPersonId, int RequestingPersonId = 0) : IRequest;

public sealed class UpdateTeamValidator : AbstractValidator<UpdateTeamCommand>
{
    public UpdateTeamValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000).When(x => x.Description is not null);
    }
}

public sealed class UpdateTeamHandler(IAppDbContext db) : IRequestHandler<UpdateTeamCommand>
{
    public async Task Handle(UpdateTeamCommand request, CancellationToken cancellationToken)
    {
        var requester = await db.Persons.FindAsync([request.RequestingPersonId], cancellationToken)
            ?? throw new KeyNotFoundException($"Persona con Id {request.RequestingPersonId} no encontrada.");
        if (requester.Role != PersonRole.Gestor)
            throw new UnauthorizedAccessException("Solo el Gestor puede actualizar equipos.");

        var team = await db.Teams.FindAsync([request.Id], cancellationToken)
            ?? throw new KeyNotFoundException($"Equipo con Id {request.Id} no encontrado.");

        if (await db.Teams.AnyAsync(t => t.Name == request.Name && t.Id != request.Id, cancellationToken))
            throw new InvalidOperationException($"Ya existe un equipo con el nombre '{request.Name}'.");

        if (request.LeadPersonId.HasValue)
        {
            var lead = await db.Persons.FindAsync([request.LeadPersonId.Value], cancellationToken);
            if (lead is null)
                throw new KeyNotFoundException($"Persona con Id {request.LeadPersonId} no encontrada.");
            if (lead.Role == PersonRole.Desarrollador)
                throw new InvalidOperationException("Un Desarrollador no puede ser líder de equipo.");
        }

        team.Update(request.Name, request.Description, request.LeadPersonId);
        await db.SaveChangesAsync(cancellationToken);
    }
}
