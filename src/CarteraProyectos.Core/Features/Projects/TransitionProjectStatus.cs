using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.Projects;

public record TransitionProjectStatusCommand(int ProjectId, ProjectStatus NewStatus, int RequestingPersonId) : IRequest;

public sealed class TransitionProjectStatusHandler(IAppDbContext db)
    : IRequestHandler<TransitionProjectStatusCommand>
{
    public async Task Handle(TransitionProjectStatusCommand request, CancellationToken cancellationToken)
    {
        var project = await db.Projects
            .FirstOrDefaultAsync(p => p.Id == request.ProjectId, cancellationToken)
            ?? throw new KeyNotFoundException($"Proyecto con Id {request.ProjectId} no encontrado.");

        var person = await db.Persons.FindAsync([request.RequestingPersonId], cancellationToken)
            ?? throw new KeyNotFoundException($"Persona con Id {request.RequestingPersonId} no encontrada.");

        await ProjectAuthorization.EnsureCanManageProjectAsync(db, project.Id, person, cancellationToken);

        if (request.NewStatus == ProjectStatus.Completed)
        {
            var hasUnfinishedSprints = await db.Sprints.AnyAsync(
                s => s.ProjectId == project.Id && s.Status != SprintStatus.Completed, cancellationToken);
            if (hasUnfinishedSprints)
                throw new InvalidOperationException("No se puede finalizar el proyecto: tiene sprints que no están en estado Completed.");

            var hasUnfinishedWorkItems = await db.WorkItems.AnyAsync(
                w => w.ProjectId == project.Id && w.Status != WorkItemStatus.Done && w.Status != WorkItemStatus.Discarded, cancellationToken);
            if (hasUnfinishedWorkItems)
                throw new InvalidOperationException("No se puede finalizar el proyecto: tiene tareas que no están en estado Done o Discarded.");
        }

        var oldStatus = project.Status;
        project.TransitionTo(request.NewStatus);
        db.ProjectStatusHistories.Add(ProjectStatusHistory.Create(project, oldStatus, request.NewStatus, request.RequestingPersonId));
        await db.SaveChangesAsync(cancellationToken);
    }
}
