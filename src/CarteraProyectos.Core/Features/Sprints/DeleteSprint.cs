using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.Sprints;

public record DeleteSprintCommand(int Id) : IRequest;

public sealed class DeleteSprintHandler(IAppDbContext db) : IRequestHandler<DeleteSprintCommand>
{
    public async Task Handle(DeleteSprintCommand request, CancellationToken cancellationToken)
    {
        var sprint = await db.Sprints.FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Sprint {request.Id} no encontrado.");

        if (sprint.Status != SprintStatus.Planning)
            throw new InvalidOperationException("Solo se puede eliminar un sprint en estado Planning.");

        var hasWorkItems = await db.WorkItems.AnyAsync(w => w.SprintId == sprint.Id, cancellationToken);
        if (hasWorkItems)
            throw new InvalidOperationException("No se puede eliminar el sprint: tiene tareas asignadas. Desasígnalas o muévelas a otro sprint primero.");

        db.Sprints.Remove(sprint);
        await db.SaveChangesAsync(cancellationToken);
    }
}
