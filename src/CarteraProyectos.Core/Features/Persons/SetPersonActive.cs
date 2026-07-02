using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Interfaces;
using FluentValidation;
using MediatR;

namespace CarteraProyectos.Core.Features.Persons;

public record SetPersonActiveCommand(int PersonId, bool IsActive, int RequestingPersonId = 0) : IRequest;

public sealed class SetPersonActiveValidator : AbstractValidator<SetPersonActiveCommand>
{
    public SetPersonActiveValidator()
    {
        RuleFor(x => x.PersonId).GreaterThan(0);
    }
}

public sealed class SetPersonActiveHandler(IAppDbContext db) : IRequestHandler<SetPersonActiveCommand>
{
    public async Task Handle(SetPersonActiveCommand request, CancellationToken cancellationToken)
    {
        var requester = await db.Persons.FindAsync([request.RequestingPersonId], cancellationToken)
            ?? throw new KeyNotFoundException($"Persona con Id {request.RequestingPersonId} no encontrada.");
        
        if (requester.Role != PersonRole.Gestor)
            throw new UnauthorizedAccessException("Solo el Gestor puede cambiar el estado de las personas.");

        var person = await db.Persons.FindAsync([request.PersonId], cancellationToken)
            ?? throw new KeyNotFoundException($"Persona con Id {request.PersonId} no encontrada.");

        if (!request.IsActive && request.PersonId == request.RequestingPersonId)
            throw new InvalidOperationException("No puedes desactivarte a ti mismo.");

        if (request.IsActive)
            person.Activate();
        else
            person.Deactivate();

        await db.SaveChangesAsync(cancellationToken);
    }
}
