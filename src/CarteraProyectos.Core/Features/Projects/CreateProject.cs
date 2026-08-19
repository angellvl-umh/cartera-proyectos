using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Interfaces;
using FluentValidation;
using MediatR;

namespace CarteraProyectos.Core.Features.Projects;

public record CreateProjectCommand(
    string Title,
    string? Description,
    string? RequestingUnit,
    ProjectComplexity Complexity,
    int? PortfolioYear,
    DateOnly? StartDate,
    DateOnly? EndDate,
    int RequestingPersonId = 0,
    int? PreviousReferenceId = null,
    int? BeneficiaryCount = null,
    int? PromoterId = null,
    int? OrganicUnitId = null,
    int? UorOrder = null,
    int? GroupPriority = null,
    DateOnly? DesiredDeploymentDate = null,
    string? SpecificationsUrl = null,
    string? EpicUrl = null,
    decimal? EstimatedBudget = null,
    int? BusinessValue = null,
    IReadOnlyList<int>? TagIds = null,
    IReadOnlyList<int>? TeamIds = null,
    int? PrimaryTeamId = null) : IRequest<int>;

public sealed class CreateProjectValidator : AbstractValidator<CreateProjectCommand>
{
    public CreateProjectValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Complexity).IsInEnum();
        RuleFor(x => x.Description).MaximumLength(2000).When(x => x.Description is not null);
        RuleFor(x => x.GroupPriority).InclusiveBetween(1, 5).When(x => x.GroupPriority.HasValue);
        RuleFor(x => x.SpecificationsUrl).MaximumLength(500).When(x => x.SpecificationsUrl is not null);
        RuleFor(x => x.EpicUrl).MaximumLength(500).When(x => x.EpicUrl is not null);
        RuleFor(x => x.BusinessValue).InclusiveBetween(1, 5).When(x => x.BusinessValue.HasValue);
        RuleFor(x => x.PrimaryTeamId)
            .Must((cmd, id) => id is null || (cmd.TeamIds ?? Array.Empty<int>()).Contains(id.Value))
            .WithMessage("El equipo primario debe estar entre los equipos asignados.");
    }
}

public sealed class CreateProjectHandler(IProjectLifecycleService service) : IRequestHandler<CreateProjectCommand, int>
{
    public Task<int> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
        => service.CreateAsync(request, cancellationToken);
}
