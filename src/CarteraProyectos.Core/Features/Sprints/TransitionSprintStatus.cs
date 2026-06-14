using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.Sprints;

public record TransitionSprintStatusCommand(int Id, SprintStatus NewStatus) : IRequest;

public sealed class TransitionSprintStatusHandler(IAppDbContext db) : IRequestHandler<TransitionSprintStatusCommand>
{
    public async Task Handle(TransitionSprintStatusCommand request, CancellationToken cancellationToken)
    {
        var sprint = await db.Sprints.FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Sprint {request.Id} no encontrado.");

        if (request.NewStatus == SprintStatus.Active)
        {
            var hasActiveSprint = await db.Sprints.AnyAsync(
                s => s.ProjectId == sprint.ProjectId && s.Status == SprintStatus.Active && s.Id != sprint.Id,
                cancellationToken);
            if (hasActiveSprint)
                throw new InvalidOperationException("Ya existe un sprint activo en este proyecto. Complétalo antes de iniciar uno nuevo.");
        }

        sprint.TransitionStatus(request.NewStatus);
        await db.SaveChangesAsync(cancellationToken);
    }
}
