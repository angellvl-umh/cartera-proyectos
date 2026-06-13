using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.Persons;

public record UpdatePersonRoleCommand(int PersonId, PersonRole Role) : IRequest;

public sealed class UpdatePersonRoleValidator : AbstractValidator<UpdatePersonRoleCommand>
{
    public UpdatePersonRoleValidator()
    {
        RuleFor(x => x.Role).IsInEnum();
    }
}

public sealed class UpdatePersonRoleHandler(IAppDbContext db) : IRequestHandler<UpdatePersonRoleCommand>
{
    public async Task Handle(UpdatePersonRoleCommand request, CancellationToken cancellationToken)
    {
        var person = await db.Persons.FindAsync([request.PersonId], cancellationToken)
            ?? throw new KeyNotFoundException($"Persona con Id {request.PersonId} no encontrada.");

        if (request.Role == PersonRole.Desarrollador)
        {
            var isTeamLead = await db.Teams.AnyAsync(t => t.LeadPersonId == request.PersonId, cancellationToken);
            if (isTeamLead)
                throw new InvalidOperationException("No se puede cambiar el rol a Desarrollador de alguien que es líder de un equipo.");
        }

        person.UpdateRole(request.Role);
        await db.SaveChangesAsync(cancellationToken);
    }
}
