using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Interfaces;
using MediatR;

namespace CarteraProyectos.Core.Features.Projects;

public record DeleteProjectCommand(int Id) : IRequest;

public sealed class DeleteProjectHandler(IAppDbContext db) : IRequestHandler<DeleteProjectCommand>
{
    public async Task Handle(DeleteProjectCommand request, CancellationToken cancellationToken)
    {
        var project = await db.Projects.FindAsync([request.Id], cancellationToken)
            ?? throw new KeyNotFoundException($"Proyecto con Id {request.Id} no encontrado.");

        if (project.Status != ProjectStatus.Proposed && project.Status != ProjectStatus.Cancelled)
            throw new InvalidOperationException("Solo se pueden eliminar proyectos en estado Propuesto o Cancelado.");

        db.Projects.Remove(project);
        await db.SaveChangesAsync(cancellationToken);
    }
}
