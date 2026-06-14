using CarteraProyectos.Core.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.Epics;

public record UpdateEpicCommand(
    int Id, string Title, string? Description,
    int Priority, int SortOrder,
    int? EstimationHours = null, int? EstimationPoints = null) : IRequest;

public sealed class UpdateEpicValidator : AbstractValidator<UpdateEpicCommand>
{
    public UpdateEpicValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Description).MaximumLength(2000).When(x => x.Description is not null);
        RuleFor(x => x.Priority).GreaterThanOrEqualTo(0);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
        RuleFor(x => x.EstimationHours).GreaterThan(0).When(x => x.EstimationHours.HasValue);
        RuleFor(x => x.EstimationPoints).GreaterThan(0).When(x => x.EstimationPoints.HasValue);
    }
}

public sealed class UpdateEpicHandler(IAppDbContext db) : IRequestHandler<UpdateEpicCommand>
{
    public async Task Handle(UpdateEpicCommand request, CancellationToken cancellationToken)
    {
        var epic = await db.Epics.FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Épica {request.Id} no encontrada.");

        epic.Update(request.Title, request.Description, request.Priority, request.SortOrder,
            request.EstimationHours, request.EstimationPoints);
        await db.SaveChangesAsync(cancellationToken);
    }
}
