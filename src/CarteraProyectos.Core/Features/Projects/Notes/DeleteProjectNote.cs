using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.Projects.Notes;

public record DeleteProjectNoteCommand(int ProjectId, int NoteId, int RequestingPersonId) : IRequest;

public sealed class DeleteProjectNoteHandler(IAppDbContext db) : IRequestHandler<DeleteProjectNoteCommand>
{
    public async Task Handle(DeleteProjectNoteCommand request, CancellationToken cancellationToken)
    {
        var note = await db.ProjectNotes
            .FirstOrDefaultAsync(n => n.Id == request.NoteId && n.ProjectId == request.ProjectId, cancellationToken)
            ?? throw new KeyNotFoundException($"Nota con Id {request.NoteId} no encontrada en el proyecto {request.ProjectId}.");

        var requester = await db.Persons.FindAsync([request.RequestingPersonId], cancellationToken)
            ?? throw new KeyNotFoundException($"Persona con Id {request.RequestingPersonId} no encontrada.");

        if (requester.Role != PersonRole.Gestor && note.AuthorId != request.RequestingPersonId)
            throw new UnauthorizedAccessException("Solo el autor o el Gestor puede eliminar esta nota.");

        db.ProjectNotes.Remove(note);
        await db.SaveChangesAsync(cancellationToken);
    }
}
