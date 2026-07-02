using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Features.Projects;
using CarteraProyectos.Core.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

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

public sealed class CreateProjectRiskHandler(IAppDbContext db) : IRequestHandler<CreateProjectRiskCommand, int>
{
    public async Task<int> Handle(CreateProjectRiskCommand request, CancellationToken cancellationToken)
    {
        if (!await db.Projects.AnyAsync(p => p.Id == request.ProjectId, cancellationToken))
            throw new KeyNotFoundException($"Proyecto con Id {request.ProjectId} no encontrado.");

        var requester = await db.Persons.FindAsync([request.RequestingPersonId], cancellationToken)
            ?? throw new KeyNotFoundException($"Persona con Id {request.RequestingPersonId} no encontrada.");

        await ProjectAuthorization.EnsureCanManageProjectAsync(db, request.ProjectId, requester, cancellationToken);

        var risk = ProjectRisk.Create(
            request.ProjectId, request.Description,
            request.Probability, request.Impact,
            request.MitigationPlan, request.RequestingPersonId);

        db.ProjectRisks.Add(risk);
        await db.SaveChangesAsync(cancellationToken);
        return risk.Id;
    }
}
