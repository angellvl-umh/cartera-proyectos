using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Interfaces;
using FluentValidation;
using MediatR;

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

public sealed class CreatePersonHandler(IPersonManagementService service)
    : IRequestHandler<CreatePersonCommand, CreatePersonResult>
{
    public Task<CreatePersonResult> Handle(CreatePersonCommand request, CancellationToken cancellationToken)
        => service.CreateAsync(request, cancellationToken);
}
