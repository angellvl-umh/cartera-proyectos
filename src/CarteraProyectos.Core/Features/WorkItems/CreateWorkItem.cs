using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.WorkItems;

public record CreateWorkItemCommand(
    int ProjectId,
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
    int? EstimationPoints = null,
    WorkItemType Type = WorkItemType.Task,
    int RequestingPersonId = 0) : IRequest<int>;

public sealed class CreateWorkItemValidator : AbstractValidator<CreateWorkItemCommand>
{
    public CreateWorkItemValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Description).MaximumLength(2000).When(x => x.Description is not null);
        RuleFor(x => x.Priority).IsInEnum();
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
        RuleFor(x => x.EstimationHours).GreaterThan(0).When(x => x.EstimationHours.HasValue);
        RuleFor(x => x.EstimationPoints).GreaterThan(0).When(x => x.EstimationPoints.HasValue);
        RuleFor(x => x.HitoDate).NotNull().When(x => x.IsHito)
            .WithMessage("Un hito debe tener fecha.");
    }
}

public sealed class CreateWorkItemHandler(IAppDbContext db) : IRequestHandler<CreateWorkItemCommand, int>
{
    public async Task<int> Handle(CreateWorkItemCommand request, CancellationToken cancellationToken)
    {
        var projectExists = await db.Projects.AnyAsync(p => p.Id == request.ProjectId, cancellationToken);
        if (!projectExists) throw new KeyNotFoundException($"Proyecto {request.ProjectId} no encontrado.");

        if (request.EpicId.HasValue)
        {
            var epicExists = await db.Epics.AnyAsync(e => e.Id == request.EpicId && e.ProjectId == request.ProjectId, cancellationToken);
            if (!epicExists) throw new KeyNotFoundException($"Épica {request.EpicId} no encontrada en el proyecto.");
        }

        if (request.SprintId.HasValue)
        {
            var sprintExists = await db.Sprints.AnyAsync(s => s.Id == request.SprintId && s.ProjectId == request.ProjectId, cancellationToken);
            if (!sprintExists) throw new KeyNotFoundException($"Sprint {request.SprintId} no encontrado en el proyecto.");
        }

        var workItem = WorkItem.Create(request.ProjectId, request.Title, request.Description,
            request.Priority, request.EpicId, request.SortOrder,
            request.EstimationHours, request.IsHito, request.HitoDate, request.DueDate,
            request.SprintId, request.EstimationPoints, request.Type);

        db.WorkItems.Add(workItem);

        if (request.AssigneeIds.Count > 0)
        {
            var persons = await db.Persons
                .Where(p => request.AssigneeIds.Contains(p.Id))
                .ToListAsync(cancellationToken);
            foreach (var person in persons)
                workItem.Assignees.Add(person);
        }

        db.WorkItemStatusHistories.Add(
            WorkItemStatusHistory.Create(workItem, null, workItem.Status, request.RequestingPersonId));

        await db.SaveChangesAsync(cancellationToken);
        return workItem.Id;
    }
}
