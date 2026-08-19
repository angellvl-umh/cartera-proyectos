using FluentValidation;
using MediatR;

namespace CarteraProyectos.Core.Features.Sprints;

public record CreateSprintCommand(
    int ProjectId, string Name, string? Goal,
    DateOnly? StartDate, DateOnly? EndDate, int? Capacity,
    int RequestingPersonId = 0) : IRequest<int>;

public sealed class CreateSprintValidator : AbstractValidator<CreateSprintCommand>
{
    public CreateSprintValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Goal).MaximumLength(1000).When(x => x.Goal is not null);
        RuleFor(x => x.Capacity).GreaterThan(0).When(x => x.Capacity.HasValue);
    }
}

public sealed class CreateSprintHandler(ISprintLifecycleService service) : IRequestHandler<CreateSprintCommand, int>
{
    public Task<int> Handle(CreateSprintCommand request, CancellationToken cancellationToken)
        => service.CreateAsync(request, cancellationToken);
}
