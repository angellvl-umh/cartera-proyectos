using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.Promoters;

public record DeletePromoterCommand(int Id, int RequestingPersonId = 0) : IRequest;

public sealed class DeletePromoterHandler(IAppDbContext db) : IRequestHandler<DeletePromoterCommand>
{
    public async Task Handle(DeletePromoterCommand request, CancellationToken cancellationToken)
    {
        var requester = await db.Persons.FindAsync([request.RequestingPersonId], cancellationToken)
            ?? throw new KeyNotFoundException($"Persona con Id {request.RequestingPersonId} no encontrada.");
        if (requester.Role != PersonRole.Gestor)
            throw new UnauthorizedAccessException("Solo el Gestor puede gestionar promotores.");

        var promoter = await db.Promoters.FindAsync([request.Id], cancellationToken)
            ?? throw new KeyNotFoundException($"Promotor con Id {request.Id} no encontrado.");

        if (await db.Projects.AnyAsync(p => p.PromoterId == request.Id, cancellationToken))
            throw new InvalidOperationException("No se puede eliminar un promotor asignado a proyectos.");

        db.Promoters.Remove(promoter);
        await db.SaveChangesAsync(cancellationToken);
    }
}
