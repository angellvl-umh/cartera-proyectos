using CarteraProyectos.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.WorkItems;

public record AssignWorkItemToSprintCommand(int WorkItemId, int ProjectId, int? SprintId) : IRequest;

public sealed class AssignWorkItemToSprintHandler(IAppDbContext db) : IRequestHandler<AssignWorkItemToSprintCommand>
{
    public async Task Handle(AssignWorkItemToSprintCommand request, CancellationToken cancellationToken)
    {
        var workItem = await db.WorkItems
            .FirstOrDefaultAsync(w => w.Id == request.WorkItemId && w.ProjectId == request.ProjectId, cancellationToken)
            ?? throw new KeyNotFoundException($"Tarea {request.WorkItemId} no encontrada.");

        if (request.SprintId.HasValue)
        {
            var sprintExists = await db.Sprints.AnyAsync(
                s => s.Id == request.SprintId && s.ProjectId == request.ProjectId, cancellationToken);
            if (!sprintExists) throw new KeyNotFoundException($"Sprint {request.SprintId} no encontrado en el proyecto.");
        }

        workItem.AssignToSprint(request.SprintId);
        await db.SaveChangesAsync(cancellationToken);
    }
}
