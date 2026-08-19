using CarteraProyectos.Core.Domain;
using FluentValidation;
using MediatR;

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

public sealed class UpdatePersonHandler(IPersonManagementService service)
    : IRequestHandler<UpdatePersonCommand>
{
    public Task Handle(UpdatePersonCommand request, CancellationToken cancellationToken)
        => service.UpdateAsync(request, cancellationToken);
}
