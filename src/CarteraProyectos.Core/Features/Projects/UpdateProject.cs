using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.Projects;

public record UpdateProjectCommand(
    int Id,
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
    int? PrimaryTeamId = null) : IRequest;

public sealed class UpdateProjectValidator : AbstractValidator<UpdateProjectCommand>
{
    public UpdateProjectValidator()
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

public sealed class UpdateProjectHandler(IAppDbContext db) : IRequestHandler<UpdateProjectCommand>
{
    public async Task Handle(UpdateProjectCommand request, CancellationToken cancellationToken)
    {
        if (request.RequestingPersonId > 0)
        {
            var requester = await db.Persons.FindAsync([request.RequestingPersonId], cancellationToken)
                ?? throw new KeyNotFoundException($"Persona con Id {request.RequestingPersonId} no encontrada.");
            if (requester.Role != PersonRole.Gestor)
                throw new UnauthorizedAccessException("Solo el Gestor puede actualizar proyectos.");
        }

        var project = await db.Projects
            .Include(p => p.Tags)
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Proyecto con Id {request.Id} no encontrado.");

        project.Update(
            request.Title, request.Description, request.RequestingUnit,
            request.Complexity, request.PortfolioYear, request.StartDate, request.EndDate,
            request.PreviousReferenceId, request.BeneficiaryCount,
            request.PromoterId, request.OrganicUnitId, request.UorOrder,
            request.GroupPriority,
            request.DesiredDeploymentDate, request.SpecificationsUrl, request.EpicUrl,
            request.EstimatedBudget, request.BusinessValue);

        if (request.TagIds is not null)
        {
            var existingTags = ((ICollection<Tag>)project.Tags);
            existingTags.Clear();
            if (request.TagIds.Count > 0)
            {
                var tags = db.Tags.Where(t => request.TagIds.Contains(t.Id)).ToList();
                foreach (var tag in tags)
                    existingTags.Add(tag);
            }
        }

        if (request.TeamIds is not null)
        {
            var existingAssignments = await db.ProjectTeamAssignments
                .Where(a => a.ProjectId == request.Id)
                .ToListAsync(cancellationToken);
            db.ProjectTeamAssignments.RemoveRange(existingAssignments);

            foreach (var teamId in request.TeamIds)
            {
                db.ProjectTeamAssignments.Add(
                    ProjectTeamAssignment.Create(request.Id, teamId, isPrimary: teamId == request.PrimaryTeamId));
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
