using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Features.Projects;
using FluentValidation;
using MediatR;

namespace CarteraProyectos.Core.Features.Projects.Dependencies;

public record CreateProjectDependencyCommand(
    int ProjectId,
    int DependsOnProjectId,
    string? Description,
    int RequestingPersonId) : IRequest<int>;

public sealed class CreateProjectDependencyValidator : AbstractValidator<CreateProjectDependencyCommand>
{
    public CreateProjectDependencyValidator()
    {
        RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description is not null);
        RuleFor(x => x.DependsOnProjectId)
            .NotEqual(x => x.ProjectId)
            .WithMessage("Un proyecto no puede depender de sí mismo.");
    }
}

public sealed class CreateProjectDependencyHandler(IProjectGovernanceService service) : IRequestHandler<CreateProjectDependencyCommand, int>
{
    public Task<int> Handle(CreateProjectDependencyCommand request, CancellationToken cancellationToken)
        => service.AddDependencyAsync(request, cancellationToken);
}
