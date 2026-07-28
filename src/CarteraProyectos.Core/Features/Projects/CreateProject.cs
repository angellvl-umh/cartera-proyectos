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

public sealed class CreateProjectHandler(IAppDbContext db) : IRequestHandler<CreateProjectCommand, int>
{
    public async Task<int> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
    {
        if (request.RequestingPersonId > 0)
        {
            var requester = await db.Persons.FindAsync([request.RequestingPersonId], cancellationToken)
                ?? throw new KeyNotFoundException($"Persona con Id {request.RequestingPersonId} no encontrada.");
            if (requester.Role != PersonRole.Gestor)
                throw new UnauthorizedAccessException("Solo el Gestor puede crear proyectos.");
        }

        var project = Project.Create(
            request.Title, request.Description, request.RequestingUnit,
            request.Complexity, request.PortfolioYear, request.StartDate, request.EndDate,
            request.PreviousReferenceId, request.BeneficiaryCount,
            request.PromoterId, request.OrganicUnitId, request.UorOrder,
            request.GroupPriority,
            request.DesiredDeploymentDate, request.SpecificationsUrl, request.EpicUrl,
            request.EstimatedBudget, request.BusinessValue);

        if (request.TagIds is { Count: > 0 })
        {
            var tags = db.Tags.Where(t => request.TagIds.Contains(t.Id)).ToList();
            foreach (var tag in tags)
                ((ICollection<Tag>)project.Tags).Add(tag);
        }

        db.Projects.Add(project);
        db.ProjectStatusHistories.Add(ProjectStatusHistory.Create(project, null, project.Status, request.RequestingPersonId));
        await db.SaveChangesAsync(cancellationToken);

        if (request.TeamIds is { Count: > 0 })
        {
            foreach (var teamId in request.TeamIds)
            {
                db.ProjectTeamAssignments.Add(
                    ProjectTeamAssignment.Create(project.Id, teamId, isPrimary: teamId == request.PrimaryTeamId));
            }
            await db.SaveChangesAsync(cancellationToken);
        }

        return project.Id;
    }
}
