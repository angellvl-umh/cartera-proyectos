using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.Projects.Risks;

public record DeleteProjectRiskCommand(int ProjectId, int RiskId, int RequestingPersonId) : IRequest;

public sealed class DeleteProjectRiskHandler(IAppDbContext db) : IRequestHandler<DeleteProjectRiskCommand>
{
    public async Task Handle(DeleteProjectRiskCommand request, CancellationToken cancellationToken)
    {
        var risk = await db.ProjectRisks
            .FirstOrDefaultAsync(r => r.Id == request.RiskId && r.ProjectId == request.ProjectId, cancellationToken)
            ?? throw new KeyNotFoundException($"Riesgo con Id {request.RiskId} no encontrado en el proyecto {request.ProjectId}.");

        var requester = await db.Persons.FindAsync([request.RequestingPersonId], cancellationToken)
            ?? throw new KeyNotFoundException($"Persona con Id {request.RequestingPersonId} no encontrada.");

        await CreateProjectRiskHandler.AuthorizeRiskWriteAsync(db, request.ProjectId, requester, cancellationToken);

        db.ProjectRisks.Remove(risk);
        await db.SaveChangesAsync(cancellationToken);
    }
}
