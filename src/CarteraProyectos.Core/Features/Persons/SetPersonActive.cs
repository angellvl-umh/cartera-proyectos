using CarteraProyectos.Core.Domain;
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

public sealed class SetPersonActiveHandler(IPersonManagementService service)
    : IRequestHandler<SetPersonActiveCommand>
{
    public Task Handle(SetPersonActiveCommand request, CancellationToken cancellationToken)
        => service.SetActiveAsync(request, cancellationToken);
}
