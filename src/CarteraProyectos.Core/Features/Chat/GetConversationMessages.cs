using CarteraProyectos.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.Chat;

public record ChatMessageResponseDto(
    int Id,
    string Role,
    string? Content,
    string? ToolCallsJson,
    string? ToolName,
    string? ToolCallId,
    DateTimeOffset CreatedAt);

public record GetConversationMessagesQuery(int PersonId, int ConversationId)
    : IRequest<IReadOnlyList<ChatMessageResponseDto>>;

public sealed class GetConversationMessagesHandler(IAppDbContext db)
    : IRequestHandler<GetConversationMessagesQuery, IReadOnlyList<ChatMessageResponseDto>>
{
    public async Task<IReadOnlyList<ChatMessageResponseDto>> Handle(
        GetConversationMessagesQuery request, CancellationToken ct)
    {
        // Verifica que la conversación existe y pertenece al usuario
        var conversation = await db.Conversations
            .Where(c => c.Id == request.ConversationId)
            .Select(c => new { c.PersonId })
            .FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException($"Conversación {request.ConversationId} no encontrada.");

        if (conversation.PersonId != request.PersonId)
            throw new KeyNotFoundException($"Conversación {request.ConversationId} no encontrada.");

        return await db.ChatMessages
            .Where(m => m.ConversationId == request.ConversationId)
            .OrderBy(m => m.CreatedAt)
            .Select(m => new ChatMessageResponseDto(
                m.Id,
                m.Role.ToString().ToLowerInvariant(),
                m.Content,
                m.ToolCallsJson,
                m.ToolName,
                m.ToolCallId,
                m.CreatedAt))
            .ToListAsync(ct);
    }
}
