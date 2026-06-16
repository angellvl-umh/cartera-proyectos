using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.OrganicUnits;

public record DeleteOrganicUnitCommand(int Id, int RequestingPersonId = 0) : IRequest;

public sealed class DeleteOrganicUnitHandler(IAppDbContext db) : IRequestHandler<DeleteOrganicUnitCommand>
{
    public async Task Handle(DeleteOrganicUnitCommand request, CancellationToken cancellationToken)
    {
        var requester = await db.Persons.FindAsync([request.RequestingPersonId], cancellationToken)
            ?? throw new KeyNotFoundException($"Persona con Id {request.RequestingPersonId} no encontrada.");
        if (requester.Role != PersonRole.Gestor)
            throw new UnauthorizedAccessException("Solo el Gestor puede gestionar unidades orgánicas.");

        var unit = await db.OrganicUnits.FindAsync([request.Id], cancellationToken)
            ?? throw new KeyNotFoundException($"Unidad orgánica con Id {request.Id} no encontrada.");

        if (await db.Projects.AnyAsync(p => p.OrganicUnitId == request.Id, cancellationToken))
            throw new InvalidOperationException("No se puede eliminar una unidad orgánica asignada a proyectos.");

        db.OrganicUnits.Remove(unit);
        await db.SaveChangesAsync(cancellationToken);
    }
}
