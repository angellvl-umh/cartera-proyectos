using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Features.Projects;
using FluentValidation;
using MediatR;

namespace CarteraProyectos.Core.Features.Projects.Risks;

public record CreateProjectRiskCommand(
    int ProjectId,
    int RequestingPersonId,
    string Description,
    RiskLevel Probability,
    RiskLevel Impact,
    string? MitigationPlan) : IRequest<int>;

public sealed class CreateProjectRiskValidator : AbstractValidator<CreateProjectRiskCommand>
{
    public CreateProjectRiskValidator()
    {
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.MitigationPlan).MaximumLength(1000).When(x => x.MitigationPlan is not null);
        RuleFor(x => x.Probability).IsInEnum();
        RuleFor(x => x.Impact).IsInEnum();
    }
}

public sealed class CreateProjectRiskHandler(IProjectGovernanceService service) : IRequestHandler<CreateProjectRiskCommand, int>
{
    public Task<int> Handle(CreateProjectRiskCommand request, CancellationToken cancellationToken)
        => service.AddRiskAsync(request, cancellationToken);
}
