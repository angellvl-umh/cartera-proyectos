using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Interfaces;
using FluentValidation;
using MediatR;

namespace CarteraProyectos.Core.Features.Projects;

public record UpdateProjectCommand(
    int Id,
    string Title,
    string? Description,
    string RequestingUnit,
    ProjectComplexity Complexity,
    int? PortfolioYear,
    DateOnly? StartDate,
    DateOnly? EndDate,
    int RequestingPersonId = 0) : IRequest;

public sealed class UpdateProjectValidator : AbstractValidator<UpdateProjectCommand>
{
    public UpdateProjectValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.RequestingUnit).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Complexity).IsInEnum();
        RuleFor(x => x.Description).MaximumLength(2000).When(x => x.Description is not null);
    }
}

public sealed class UpdateProjectHandler(IAppDbContext db) : IRequestHandler<UpdateProjectCommand>
{
    public async Task Handle(UpdateProjectCommand request, CancellationToken cancellationToken)
    {
        var requester = await db.Persons.FindAsync([request.RequestingPersonId], cancellationToken)
            ?? throw new KeyNotFoundException($"Persona con Id {request.RequestingPersonId} no encontrada.");
        if (requester.Role != PersonRole.Gestor)
            throw new UnauthorizedAccessException("Solo el Gestor puede actualizar proyectos.");

        var project = await db.Projects.FindAsync([request.Id], cancellationToken)
            ?? throw new KeyNotFoundException($"Proyecto con Id {request.Id} no encontrado.");

        project.Update(request.Title, request.Description, request.RequestingUnit,
            request.Complexity, request.PortfolioYear, request.StartDate, request.EndDate);

        await db.SaveChangesAsync(cancellationToken);
    }
}
