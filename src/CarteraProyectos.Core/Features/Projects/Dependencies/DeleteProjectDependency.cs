using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Interfaces;
using CarteraProyectos.Core.Features.Projects.Risks;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.Projects.Dependencies;

public record DeleteProjectDependencyCommand(int ProjectId, int DependencyId, int RequestingPersonId) : IRequest;

public sealed class DeleteProjectDependencyHandler(IAppDbContext db) : IRequestHandler<DeleteProjectDependencyCommand>
{
    public async Task Handle(DeleteProjectDependencyCommand request, CancellationToken cancellationToken)
    {
        var dependency = await db.ProjectDependencies
            .FirstOrDefaultAsync(d => d.Id == request.DependencyId && d.ProjectId == request.ProjectId, cancellationToken)
            ?? throw new KeyNotFoundException($"Dependencia con Id {request.DependencyId} no encontrada en el proyecto {request.ProjectId}.");

        var requester = await db.Persons.FindAsync([request.RequestingPersonId], cancellationToken)
            ?? throw new KeyNotFoundException($"Persona con Id {request.RequestingPersonId} no encontrada.");

        await CreateProjectRiskHandler.AuthorizeRiskWriteAsync(db, request.ProjectId, requester, cancellationToken);

        db.ProjectDependencies.Remove(dependency);
        await db.SaveChangesAsync(cancellationToken);
    }
}
