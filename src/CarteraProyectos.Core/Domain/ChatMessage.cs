namespace CarteraProyectos.Core.Domain;

public enum ChatMessageRole
{
    User,
    Assistant,
    Tool,
}

public class ChatMessage
{
    public int Id { get; private set; }
    public int ConversationId { get; private set; }
    public ChatMessageRole Role { get; private set; }

    /// <summary>Contenido textual del mensaje. Puede ser null en mensajes Assistant que solo emiten tool calls.</summary>
    public string? Content { get; private set; }

    /// <summary>JSON con el array de tool calls solicitados por el modelo (solo en mensajes Assistant con tool_calls).</summary>
    public string? ToolCallsJson { get; private set; }

    /// <summary>Nombre de la tool invocada (solo en mensajes Tool).</summary>
    public string? ToolName { get; private set; }

    /// <summary>ID de la tool call a la que responde (solo en mensajes Tool).</summary>
    public string? ToolCallId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public Conversation? Conversation { get; private set; }

    private ChatMessage() { }

    public static ChatMessage CreateUserMessage(int conversationId, string content)
        => new()
        {
            ConversationId = conversationId,
            Role           = ChatMessageRole.User,
            Content        = content,
            CreatedAt      = DateTimeOffset.UtcNow,
        };

    public static ChatMessage CreateAssistantMessage(int conversationId, string? content, string? toolCallsJson = null)
        => new()
        {
            ConversationId = conversationId,
            Role           = ChatMessageRole.Assistant,
            Content        = content,
            ToolCallsJson  = toolCallsJson,
            CreatedAt      = DateTimeOffset.UtcNow,
        };

    public static ChatMessage CreateToolMessage(
        int conversationId, string toolCallId, string toolName, string content)
        => new()
        {
            ConversationId = conversationId,
            Role           = ChatMessageRole.Tool,
            Content        = content,
            ToolCallId     = toolCallId,
            ToolName       = toolName,
            CreatedAt      = DateTimeOffset.UtcNow,
        };
}
