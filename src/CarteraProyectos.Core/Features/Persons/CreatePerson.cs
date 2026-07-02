using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.Persons;

public record CreatePersonCommand(string Name, string Email, PersonRole Role, int RequestingPersonId = 0) : IRequest<int>;

public sealed class CreatePersonValidator : AbstractValidator<CreatePersonCommand>
{
    public CreatePersonValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.Role).IsInEnum();
    }
}

public sealed class CreatePersonHandler(IAppDbContext db) : IRequestHandler<CreatePersonCommand, int>
{
    public async Task<int> Handle(CreatePersonCommand request, CancellationToken cancellationToken)
    {
        var requester = await db.Persons.FindAsync([request.RequestingPersonId], cancellationToken)
            ?? throw new KeyNotFoundException($"Persona con Id {request.RequestingPersonId} no encontrada.");
        
        if (requester.Role != PersonRole.Gestor)
            throw new UnauthorizedAccessException("Solo el Gestor puede crear personas.");

        var exists = await db.Persons.AnyAsync(
            p => p.Email.ToLower() == request.Email.ToLower(), 
            cancellationToken);
        
        if (exists)
            throw new InvalidOperationException("Ya existe una persona con ese email.");

        var person = Person.Create(request.Name, request.Email, request.Role);
        db.Persons.Add(person);
        await db.SaveChangesAsync(cancellationToken);
        
        return person.Id;
    }
}
