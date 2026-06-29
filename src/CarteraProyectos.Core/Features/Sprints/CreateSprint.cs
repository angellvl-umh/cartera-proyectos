using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.Sprints;

public record CreateSprintCommand(
    int ProjectId, string Name, string? Goal,
    DateOnly? StartDate, DateOnly? EndDate, int? Capacity,
    int RequestingPersonId = 0) : IRequest<int>;

public sealed class CreateSprintValidator : AbstractValidator<CreateSprintCommand>
{
    public CreateSprintValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Goal).MaximumLength(1000).When(x => x.Goal is not null);
        RuleFor(x => x.Capacity).GreaterThan(0).When(x => x.Capacity.HasValue);
    }
}

public sealed class CreateSprintHandler(IAppDbContext db) : IRequestHandler<CreateSprintCommand, int>
{
    public async Task<int> Handle(CreateSprintCommand request, CancellationToken cancellationToken)
    {
        var projectExists = await db.Projects.AnyAsync(p => p.Id == request.ProjectId, cancellationToken);
        if (!projectExists) throw new KeyNotFoundException($"Proyecto {request.ProjectId} no encontrado.");

        var sprint = Sprint.Create(request.ProjectId, request.Name, request.Goal,
            request.StartDate, request.EndDate, request.Capacity);
        db.Sprints.Add(sprint);

        db.SprintStatusHistories.Add(
            SprintStatusHistory.Create(sprint, null, sprint.Status, request.RequestingPersonId));

        await db.SaveChangesAsync(cancellationToken);
        return sprint.Id;
    }
}
