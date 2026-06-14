using CarteraProyectos.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.Comments;

public record DeleteCommentCommand(int CommentId, int RequestingPersonId) : IRequest;

public sealed class DeleteCommentHandler(IAppDbContext db) : IRequestHandler<DeleteCommentCommand>
{
    public async Task Handle(DeleteCommentCommand request, CancellationToken ct)
    {
        var comment = await db.Comments.FindAsync([request.CommentId], ct);
        if (comment is null) throw new KeyNotFoundException($"Comentario {request.CommentId} no encontrado.");
        if (comment.AuthorId != request.RequestingPersonId)
            throw new UnauthorizedAccessException("Solo el autor puede eliminar su comentario.");

        db.Comments.Remove(comment);
        await db.SaveChangesAsync(ct);
    }
}
