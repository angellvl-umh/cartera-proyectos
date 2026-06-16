using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Interfaces;
using MediatR;

namespace CarteraProyectos.Core.Features.Tags;

public record DeleteTagCommand(int Id, int RequestingPersonId = 0) : IRequest;

public sealed class DeleteTagHandler(IAppDbContext db) : IRequestHandler<DeleteTagCommand>
{
    public async Task Handle(DeleteTagCommand request, CancellationToken cancellationToken)
    {
        var requester = await db.Persons.FindAsync([request.RequestingPersonId], cancellationToken)
            ?? throw new KeyNotFoundException($"Persona con Id {request.RequestingPersonId} no encontrada.");
        if (requester.Role != PersonRole.Gestor)
            throw new UnauthorizedAccessException("Solo el Gestor puede gestionar etiquetas.");

        var tag = await db.Tags.FindAsync([request.Id], cancellationToken)
            ?? throw new KeyNotFoundException($"Etiqueta con Id {request.Id} no encontrada.");

        // FK cascade on ProjectTags → Tag removes join rows automatically
        db.Tags.Remove(tag);
        await db.SaveChangesAsync(cancellationToken);
    }
}
