using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.Epics;

public record CreateEpicCommand(
    int ProjectId, string Title, string? Description,
    int Priority, int SortOrder,
    int? EstimationHours = null, int? EstimationPoints = null) : IRequest<int>;

public sealed class CreateEpicValidator : AbstractValidator<CreateEpicCommand>
{
    public CreateEpicValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Description).MaximumLength(2000).When(x => x.Description is not null);
        RuleFor(x => x.Priority).GreaterThanOrEqualTo(0);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
        RuleFor(x => x.EstimationHours).GreaterThan(0).When(x => x.EstimationHours.HasValue);
        RuleFor(x => x.EstimationPoints).GreaterThan(0).When(x => x.EstimationPoints.HasValue);
    }
}

public sealed class CreateEpicHandler(IAppDbContext db) : IRequestHandler<CreateEpicCommand, int>
{
    public async Task<int> Handle(CreateEpicCommand request, CancellationToken cancellationToken)
    {
        var projectExists = await db.Projects.AnyAsync(p => p.Id == request.ProjectId, cancellationToken);
        if (!projectExists) throw new KeyNotFoundException($"Proyecto {request.ProjectId} no encontrado.");

        var epic = Epic.Create(request.ProjectId, request.Title, request.Description,
            request.Priority, request.SortOrder, request.EstimationHours, request.EstimationPoints);
        db.Epics.Add(epic);
        await db.SaveChangesAsync(cancellationToken);
        return epic.Id;
    }
}
