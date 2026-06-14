using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.WorkItems;

public record UpdateWorkItemCommand(
    int Id,
    string Title,
    string? Description,
    WorkItemPriority Priority,
    int? EpicId,
    IReadOnlyList<int> AssigneeIds,
    int SortOrder,
    int? EstimationHours,
    bool IsHito,
    DateOnly? HitoDate,
    DateOnly? DueDate,
    int? SprintId = null,
    int? EstimationPoints = null) : IRequest;

public sealed class UpdateWorkItemValidator : AbstractValidator<UpdateWorkItemCommand>
{
    public UpdateWorkItemValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Description).MaximumLength(2000).When(x => x.Description is not null);
        RuleFor(x => x.Priority).IsInEnum();
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
        RuleFor(x => x.EstimationHours).GreaterThan(0).When(x => x.EstimationHours.HasValue);
        RuleFor(x => x.EstimationPoints).GreaterThan(0).When(x => x.EstimationPoints.HasValue);
    }
}

public sealed class UpdateWorkItemHandler(IAppDbContext db) : IRequestHandler<UpdateWorkItemCommand>
{
    public async Task Handle(UpdateWorkItemCommand request, CancellationToken cancellationToken)
    {
        var workItem = await db.WorkItems
            .Include(w => w.Assignees)
            .FirstOrDefaultAsync(w => w.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Tarea {request.Id} no encontrada.");

        workItem.Update(request.Title, request.Description, request.Priority, request.EpicId,
            request.SortOrder, request.EstimationHours, request.IsHito, request.HitoDate, request.DueDate,
            request.SprintId, request.EstimationPoints);

        workItem.Assignees.Clear();
        if (request.AssigneeIds.Count > 0)
        {
            var persons = await db.Persons
                .Where(p => request.AssigneeIds.Contains(p.Id))
                .ToListAsync(cancellationToken);
            foreach (var person in persons)
                workItem.Assignees.Add(person);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
