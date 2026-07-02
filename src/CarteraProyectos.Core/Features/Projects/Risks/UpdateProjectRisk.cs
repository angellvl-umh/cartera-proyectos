using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Features.Projects;
using CarteraProyectos.Core.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

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

public sealed class UpdateProjectRiskHandler(IAppDbContext db) : IRequestHandler<UpdateProjectRiskCommand>
{
    public async Task Handle(UpdateProjectRiskCommand request, CancellationToken cancellationToken)
    {
        var risk = await db.ProjectRisks
            .FirstOrDefaultAsync(r => r.Id == request.RiskId && r.ProjectId == request.ProjectId, cancellationToken)
            ?? throw new KeyNotFoundException($"Riesgo con Id {request.RiskId} no encontrado en el proyecto {request.ProjectId}.");

        var requester = await db.Persons.FindAsync([request.RequestingPersonId], cancellationToken)
            ?? throw new KeyNotFoundException($"Persona con Id {request.RequestingPersonId} no encontrada.");

        await ProjectAuthorization.EnsureCanManageProjectAsync(db, request.ProjectId, requester, cancellationToken);

        risk.Update(request.Description, request.Probability, request.Impact,
            request.MitigationPlan, request.Status);

        await db.SaveChangesAsync(cancellationToken);
    }
}
