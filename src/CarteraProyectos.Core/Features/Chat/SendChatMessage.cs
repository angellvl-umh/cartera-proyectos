using System.Text.Json;
using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Features.Chat.Tools;
using CarteraProyectos.Core.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.Chat;

// ─── Command + DTO ───────────────────────────────────────────────────────────

public record SendChatMessageCommand(
    int PersonId,
    int ConversationId,
    string Text) : IRequest<SendChatMessageResult>;

public record SendChatMessageResult(
    int MessageId,
    string AssistantReply,
    bool HitIterationLimit);

// ─── Validator ───────────────────────────────────────────────────────────────

public class SendChatMessageValidator : AbstractValidator<SendChatMessageCommand>
{
    public SendChatMessageValidator()
    {
        RuleFor(x => x.Text).NotEmpty().MaximumLength(32_000);
        RuleFor(x => x.PersonId).GreaterThan(0);
        RuleFor(x => x.ConversationId).GreaterThan(0);
    }
}

// ─── Handler ─────────────────────────────────────────────────────────────────

public sealed class SendChatMessageHandler(
    IAppDbContext db,
    IChatCompletionClient llm,
    ISender sender)
    : IRequestHandler<SendChatMessageCommand, SendChatMessageResult>
{
    private const int MaxIterations = 5;

    public async Task<SendChatMessageResult> Handle(
        SendChatMessageCommand request, CancellationToken ct)
    {
        // 1. Verificar que la conversación existe y pertenece al usuario
        var conversation = await db.Conversations.FindAsync([request.ConversationId], ct)
            ?? throw new KeyNotFoundException($"Conversación {request.ConversationId} no encontrada.");

        if (conversation.PersonId != request.PersonId)
            throw new KeyNotFoundException($"Conversación {request.ConversationId} no encontrada.");

        // 2. Persistir mensaje de usuario
        var userMessage = ChatMessage.CreateUserMessage(request.ConversationId, request.Text);
        db.ChatMessages.Add(userMessage);
        await db.SaveChangesAsync(ct);

        // 3. Construir historial completo (sistema + histórico + nuevo mensaje)
        var history = await BuildHistoryAsync(request.ConversationId, ct);
        var tools   = ChatToolCatalog.GetDefinitions();

        // 4. Bucle de tool-calling (tope MaxIterations)
        string? lastTextContent = null;
        bool hitLimit = false;

        for (int iter = 0; iter < MaxIterations; iter++)
        {
            var completionRequest = new Interfaces.ChatCompletionRequest(history, tools);
            var response = await llm.CompleteAsync(completionRequest, ct);

            // Guardar el mensaje assistant en BD
            string? toolCallsJson = null;
            if (response.ToolCalls is { Count: > 0 })
            {
                toolCallsJson = JsonSerializer.Serialize(response.ToolCalls, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
            }

            var assistantMsg = ChatMessage.CreateAssistantMessage(
                request.ConversationId, response.Content, toolCallsJson);
            db.ChatMessages.Add(assistantMsg);
            await db.SaveChangesAsync(ct);

            if (!string.IsNullOrEmpty(response.Content))
                lastTextContent = response.Content;

            // Si no hay tool calls o finish_reason es "stop", hemos terminado
            if (response.ToolCalls is null or { Count: 0 }
                || string.Equals(response.FinishReason, "stop", StringComparison.OrdinalIgnoreCase))
            {
                lastTextContent = response.Content ?? lastTextContent ?? string.Empty;
                break;
            }

            // Añadir el turno del assistant al historial
            history = history.Append(new Interfaces.ChatMessageDto(
                Role: "assistant",
                Content: response.Content,
                ToolCalls: response.ToolCalls)).ToList();

            // Ejecutar cada tool call y añadir resultados al historial
            foreach (var toolCall in response.ToolCalls)
            {
                string toolResult;
                try
                {
                    toolResult = await ChatToolCatalog.ExecuteAsync(
                        toolCall.ToolName, toolCall.ArgumentsJson,
                        request.PersonId, sender, ct);
                }
                catch (Exception ex)
                {
                    toolResult = JsonSerializer.Serialize(new { error = ex.Message });
                }

                // Persistir el mensaje Tool en BD
                var toolMsg = ChatMessage.CreateToolMessage(
                    request.ConversationId, toolCall.ToolCallId, toolCall.ToolName, toolResult);
                db.ChatMessages.Add(toolMsg);

                // Añadir al historial en memoria
                history = history.Append(new Interfaces.ChatMessageDto(
                    Role: "tool",
                    Content: toolResult,
                    ToolCallId: toolCall.ToolCallId,
                    ToolName: toolCall.ToolName)).ToList();
            }

            await db.SaveChangesAsync(ct);

            // Si era la última iteración y todavía hay tool calls, marcamos límite
            if (iter == MaxIterations - 1 && response.ToolCalls is { Count: > 0 })
            {
                hitLimit = true;
                lastTextContent ??= "He alcanzado el límite de pasos de razonamiento. Por favor, reformula tu pregunta o divide la tarea en pasos más simples.";
            }
        }

        // 5. Actualizar el timestamp de la conversación
        conversation.Touch();
        await db.SaveChangesAsync(ct);

        return new SendChatMessageResult(
            MessageId: userMessage.Id,
            AssistantReply: lastTextContent ?? string.Empty,
            HitIterationLimit: hitLimit);
    }

    private async Task<List<Interfaces.ChatMessageDto>> BuildHistoryAsync(
        int conversationId, CancellationToken ct)
    {
        var history = new List<Interfaces.ChatMessageDto>
        {
            new("system", ChatSystemPrompt.Base)
        };

        var messages = await db.ChatMessages
                    .Where(m => m.ConversationId == conversationId)
                    .OrderBy(m => m.CreatedAt)
                    .ToListAsync(ct);

        foreach (var msg in messages)
        {
            switch (msg.Role)
            {
                case ChatMessageRole.User:
                    history.Add(new("user", msg.Content));
                    break;

                case ChatMessageRole.Assistant:
                    List<Interfaces.ChatToolCall>? toolCalls = null;
                    if (!string.IsNullOrEmpty(msg.ToolCallsJson))
                    {
                        try
                        {
                            toolCalls = JsonSerializer.Deserialize<List<Interfaces.ChatToolCall>>(
                                msg.ToolCallsJson,
                                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        }
                        catch { /* ignorar JSON malformado */ }
                    }
                    history.Add(new("assistant", msg.Content, ToolCalls: toolCalls));
                    break;

                case ChatMessageRole.Tool:
                    history.Add(new("tool", msg.Content,
                        ToolCallId: msg.ToolCallId,
                        ToolName: msg.ToolName));
                    break;
            }
        }

        return history;
    }
}
