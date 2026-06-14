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

        db.Sprints.Remove(sprint);
        await db.SaveChangesAsync(cancellationToken);
    }
}
