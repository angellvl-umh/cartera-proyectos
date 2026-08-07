using CarteraProyectos.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.Chat;

public record DeleteConversationCommand(int PersonId, int ConversationId) : IRequest;

public sealed class DeleteConversationHandler(IAppDbContext db)
    : IRequestHandler<DeleteConversationCommand>
{
    public async Task Handle(DeleteConversationCommand request, CancellationToken ct)
    {
        var conversation = await db.Conversations
            .FirstOrDefaultAsync(c => c.Id == request.ConversationId, ct)
            ?? throw new KeyNotFoundException($"Conversación {request.ConversationId} no encontrada.");

        if (conversation.PersonId != request.PersonId)
            throw new KeyNotFoundException($"Conversación {request.ConversationId} no encontrada.");

        db.Conversations.Remove(conversation);
        await db.SaveChangesAsync(ct);
    }
}
