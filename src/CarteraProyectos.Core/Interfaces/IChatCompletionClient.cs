namespace CarteraProyectos.Core.Interfaces;

// ─── DTOs propios de Core (sin dependencia del SDK OpenAI) ──────────────────

/// <summary>Definición de una tool que el modelo puede invocar.</summary>
public record ChatToolDefinition(
    string Name,
    string Description,
    string ParametersJson   // JSON Schema como string
);

/// <summary>Llamada a tool solicitada por el modelo en un mensaje Assistant.</summary>
public record ChatToolCall(
    string ToolCallId,
    string ToolName,
    string ArgumentsJson    // JSON de los argumentos tal como los devuelve el modelo
);

/// <summary>Mensaje del historial de conversación.</summary>
public record ChatMessageDto(
    string Role,            // "user" | "assistant" | "tool" | "system"
    string? Content,
    IReadOnlyList<ChatToolCall>? ToolCalls = null,
    string? ToolCallId = null,  // solo en role="tool"
    string? ToolName = null     // solo en role="tool"
);

/// <summary>Petición al cliente de completación de chat.</summary>
public record ChatCompletionRequest(
    IReadOnlyList<ChatMessageDto> Messages,
    IReadOnlyList<ChatToolDefinition>? Tools = null
);

/// <summary>Respuesta del cliente de completación de chat.</summary>
public record ChatCompletionResponse(
    string Role,            // siempre "assistant"
    string? Content,        // texto, null si el modelo solo emite tool calls
    IReadOnlyList<ChatToolCall>? ToolCalls,
    string FinishReason     // "stop" | "tool_calls" | "length" | etc.
);

// ─── Contrato ────────────────────────────────────────────────────────────────

/// <summary>
/// Abstracción para completación de chat con soporte de tool calling.
/// La implementación concreta (LiteLLM/OpenAI SDK) vive en Infrastructure.
/// </summary>
public interface IChatCompletionClient
{
    Task<ChatCompletionResponse> CompleteAsync(
        ChatCompletionRequest request,
        CancellationToken ct = default);
}
