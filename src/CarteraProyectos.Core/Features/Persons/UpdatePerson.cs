using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.Persons;

public record UpdatePersonCommand(int PersonId, string Name, string Email, PersonRole Role, int RequestingPersonId = 0) : IRequest;

public sealed class UpdatePersonValidator : AbstractValidator<UpdatePersonCommand>
{
    public UpdatePersonValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.Role).IsInEnum();
    }
}

public sealed class UpdatePersonHandler(IAppDbContext db) : IRequestHandler<UpdatePersonCommand>
{
    public async Task Handle(UpdatePersonCommand request, CancellationToken cancellationToken)
    {
        var requester = await db.Persons.FindAsync([request.RequestingPersonId], cancellationToken)
            ?? throw new KeyNotFoundException($"Persona con Id {request.RequestingPersonId} no encontrada.");
        
        if (requester.Role != PersonRole.Gestor)
            throw new UnauthorizedAccessException("Solo el Gestor puede actualizar personas.");

        var person = await db.Persons.FindAsync([request.PersonId], cancellationToken)
            ?? throw new KeyNotFoundException($"Persona con Id {request.PersonId} no encontrada.");

        // Verificar email duplicado (de otra persona)
        var emailExists = await db.Persons.AnyAsync(
            p => p.Id != request.PersonId && p.Email.ToLower() == request.Email.ToLower(),
            cancellationToken);
        
        if (emailExists)
            throw new InvalidOperationException("Ya existe otra persona con ese email.");

        person.Update(request.Name, request.Email);
        person.UpdateRole(request.Role);
        
        await db.SaveChangesAsync(cancellationToken);
    }
}
