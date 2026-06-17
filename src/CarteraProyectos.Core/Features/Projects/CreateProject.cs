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
    SiptGroup? SiptGroup = null,
    DateOnly? DesiredDeploymentDate = null,
    string? SpecificationsUrl = null,
    string? EpicUrl = null,
    decimal? EstimatedBudget = null,
    IReadOnlyList<int>? TagIds = null) : IRequest<int>;

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
            request.GroupPriority, request.SiptGroup,
            request.DesiredDeploymentDate, request.SpecificationsUrl, request.EpicUrl,
            request.EstimatedBudget);

        if (request.TagIds is { Count: > 0 })
        {
            var tags = db.Tags.Where(t => request.TagIds.Contains(t.Id)).ToList();
            foreach (var tag in tags)
                ((ICollection<Tag>)project.Tags).Add(tag);
        }

        db.Projects.Add(project);
        await db.SaveChangesAsync(cancellationToken);
        return project.Id;
    }
}
