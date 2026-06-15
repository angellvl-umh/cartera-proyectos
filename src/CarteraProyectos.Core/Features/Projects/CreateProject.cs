using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Interfaces;
using FluentValidation;
using MediatR;

namespace CarteraProyectos.Core.Features.Projects;

public record CreateProjectCommand(
    string Title,
    string? Description,
    string RequestingUnit,
    ProjectComplexity Complexity,
    int? PortfolioYear,
    DateOnly? StartDate,
    DateOnly? EndDate,
    int RequestingPersonId = 0) : IRequest<int>;

public sealed class CreateProjectValidator : AbstractValidator<CreateProjectCommand>
{
    public CreateProjectValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.RequestingUnit).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Complexity).IsInEnum();
        RuleFor(x => x.Description).MaximumLength(2000).When(x => x.Description is not null);
    }
}

public sealed class CreateProjectHandler(IAppDbContext db) : IRequestHandler<CreateProjectCommand, int>
{
    public async Task<int> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
    {
        var requester = await db.Persons.FindAsync([request.RequestingPersonId], cancellationToken)
            ?? throw new KeyNotFoundException($"Persona con Id {request.RequestingPersonId} no encontrada.");
        if (requester.Role != PersonRole.Gestor)
            throw new UnauthorizedAccessException("Solo el Gestor puede crear proyectos.");

        var project = Project.Create(
            request.Title, request.Description, request.RequestingUnit,
            request.Complexity, request.PortfolioYear, request.StartDate, request.EndDate);

        db.Projects.Add(project);
        await db.SaveChangesAsync(cancellationToken);
        return project.Id;
    }
}
