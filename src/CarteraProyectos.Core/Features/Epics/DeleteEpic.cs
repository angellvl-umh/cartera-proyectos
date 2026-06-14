using CarteraProyectos.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.Epics;

public record DeleteEpicCommand(int Id) : IRequest;

public sealed class DeleteEpicHandler(IAppDbContext db) : IRequestHandler<DeleteEpicCommand>
{
    public async Task Handle(DeleteEpicCommand request, CancellationToken cancellationToken)
    {
        var epic = await db.Epics.FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Épica {request.Id} no encontrada.");

        db.Epics.Remove(epic);
        await db.SaveChangesAsync(cancellationToken);
    }
}
