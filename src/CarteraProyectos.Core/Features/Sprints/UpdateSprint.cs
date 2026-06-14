using CarteraProyectos.Core.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.Sprints;

public record UpdateSprintCommand(
    int Id, string Name, string? Goal,
    DateOnly? StartDate, DateOnly? EndDate, int? Capacity) : IRequest;

public sealed class UpdateSprintValidator : AbstractValidator<UpdateSprintCommand>
{
    public UpdateSprintValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Goal).MaximumLength(1000).When(x => x.Goal is not null);
        RuleFor(x => x.Capacity).GreaterThan(0).When(x => x.Capacity.HasValue);
    }
}

public sealed class UpdateSprintHandler(IAppDbContext db) : IRequestHandler<UpdateSprintCommand>
{
    public async Task Handle(UpdateSprintCommand request, CancellationToken cancellationToken)
    {
        var sprint = await db.Sprints.FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Sprint {request.Id} no encontrado.");

        sprint.Update(request.Name, request.Goal, request.StartDate, request.EndDate, request.Capacity);
        await db.SaveChangesAsync(cancellationToken);
    }
}
