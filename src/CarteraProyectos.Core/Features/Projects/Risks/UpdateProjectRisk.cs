using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Features.Projects;
using FluentValidation;
using MediatR;

namespace CarteraProyectos.Core.Features.Projects.Risks;

public record UpdateProjectRiskCommand(
    int ProjectId,
    int RiskId,
    int RequestingPersonId,
    string Description,
    RiskLevel Probability,
    RiskLevel Impact,
    string? MitigationPlan,
    RiskStatus Status) : IRequest;

public sealed class UpdateProjectRiskValidator : AbstractValidator<UpdateProjectRiskCommand>
{
    public UpdateProjectRiskValidator()
    {
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.MitigationPlan).MaximumLength(1000).When(x => x.MitigationPlan is not null);
        RuleFor(x => x.Probability).IsInEnum();
        RuleFor(x => x.Impact).IsInEnum();
        RuleFor(x => x.Status).IsInEnum();
    }
}

public sealed class UpdateProjectRiskHandler(IProjectGovernanceService service) : IRequestHandler<UpdateProjectRiskCommand>
{
    public Task Handle(UpdateProjectRiskCommand request, CancellationToken cancellationToken)
        => service.UpdateRiskAsync(request, cancellationToken);
}
