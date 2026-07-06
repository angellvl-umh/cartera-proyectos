using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.Persons;

public record CreatePersonCommand(
    string Name,
    string Email,
    PersonRole Role,
    int RequestingPersonId = 0,
    bool CreateLocalCredentials = false) : IRequest<CreatePersonResult>;

public record CreatePersonResult(int Id, string? TemporaryPassword, string? CredentialsWarning);

public sealed class CreatePersonValidator : AbstractValidator<CreatePersonCommand>
{
    public CreatePersonValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.Role).IsInEnum();
    }
}

public sealed class CreatePersonHandler(IAppDbContext db, IIdentityProviderService idp)
    : IRequestHandler<CreatePersonCommand, CreatePersonResult>
{
    public async Task<CreatePersonResult> Handle(CreatePersonCommand request, CancellationToken cancellationToken)
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

        if (!request.CreateLocalCredentials)
            return new CreatePersonResult(person.Id, null, null);

        var idpResult = await idp.CreateUserWithTemporaryPasswordAsync(request.Name, request.Email, cancellationToken);

        return idpResult.Status switch
        {
            IdentityCredentialsStatus.Created =>
                new CreatePersonResult(person.Id, idpResult.TemporaryPassword, null),

            IdentityCredentialsStatus.AlreadyExists =>
                new CreatePersonResult(
                    person.Id,
                    null,
                    "Ya existía una cuenta en el proveedor de identidad para ese email; no se ha creado una nueva."),

            _ => // Unavailable
                new CreatePersonResult(
                    person.Id,
                    null,
                    "La persona se ha creado, pero no se pudo crear la cuenta local en el proveedor de identidad. " +
                    "Puede crearse más tarde o el usuario puede entrar con Google/SSO."),
        };
    }
}
