using System.ClientModel;
using System.Text.Json;
using CarteraProyectos.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Chat;

namespace CarteraProyectos.Infrastructure.Services;

/// <summary>
/// Implementación de <see cref="IChatCompletionClient"/> usando el SDK oficial OpenAI
/// apuntando al proxy LiteLLM (OpenAI-compatible).
/// </summary>
public sealed class LiteLlmChatCompletionClient : IChatCompletionClient
{
    private readonly ChatClient _client;
    private readonly ILogger<LiteLlmChatCompletionClient> _logger;

    public LiteLlmChatCompletionClient(IConfiguration config, ILogger<LiteLlmChatCompletionClient> logger)
    {
        _logger = logger;

        var baseUrl = config["LiteLlm:BaseUrl"] ?? "http://localhost:4000";
        var apiKey  = config["LiteLlm:ApiKey"]  ?? "dummy";
        var model   = config["Chat:Model"]       ?? "claude-sonnet";

        var openAiClient = new OpenAIClient(
            new ApiKeyCredential(apiKey),
            new OpenAIClientOptions { Endpoint = new Uri(baseUrl) });

        _client = openAiClient.GetChatClient(model);
    }

    public async Task<ChatCompletionResponse> CompleteAsync(
        ChatCompletionRequest request, CancellationToken ct = default)
    {
        try
        {
            var messages = BuildMessages(request.Messages);
            var options  = BuildOptions(request.Tools);
            var sdkResponse = await _client.CompleteChatAsync(messages, options, ct);
            var completion  = sdkResponse.Value;

            return MapResponse(completion);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling LiteLLM chat completion");
            throw;
        }
    }

    // ── Conversión Core DTOs → SDK ────────────────────────────────────────────

    private static List<ChatMessage> BuildMessages(IReadOnlyList<ChatMessageDto> dtos)
    {
        var messages = new List<ChatMessage>(dtos.Count);

        foreach (var msg in dtos)
        {
            switch (msg.Role.ToLowerInvariant())
            {
                case "system":
                    messages.Add(new SystemChatMessage(msg.Content ?? string.Empty));
                    break;

                case "user":
                    messages.Add(new UserChatMessage(msg.Content ?? string.Empty));
                    break;

                case "assistant":
                    if (msg.ToolCalls is { Count: > 0 })
                    {
                        var toolCallParts = msg.ToolCalls.Select(tc =>
                            OpenAI.Chat.ChatToolCall.CreateFunctionToolCall(
                                tc.ToolCallId, tc.ToolName, BinaryData.FromString(tc.ArgumentsJson)))
                            .ToList();

                        // AssistantChatMessage constructor acepta tool calls como primer argumento
                        messages.Add(new AssistantChatMessage(toolCallParts));
                    }
                    else
                    {
                        messages.Add(new AssistantChatMessage(msg.Content ?? string.Empty));
                    }
                    break;

                case "tool":
                    messages.Add(new ToolChatMessage(
                        msg.ToolCallId ?? string.Empty,
                        msg.Content ?? string.Empty));
                    break;

                default:
                    // Ignorar roles desconocidos
                    break;
            }
        }

        return messages;
    }

    private static ChatCompletionOptions? BuildOptions(IReadOnlyList<ChatToolDefinition>? tools)
    {
        if (tools is null || tools.Count == 0)
            return null;

        var options = new ChatCompletionOptions();

        foreach (var tool in tools)
        {
            var schema = BinaryData.FromString(tool.ParametersJson);
            options.Tools.Add(ChatTool.CreateFunctionTool(
                functionName: tool.Name,
                functionDescription: tool.Description,
                functionParameters: schema));
        }

        return options;
    }

    // ── Conversión SDK → Core DTOs ────────────────────────────────────────────

    private static ChatCompletionResponse MapResponse(ChatCompletion completion)
    {
        var content = completion.Content.Count > 0 ? completion.Content[0].Text : null;

        // ChatFinishReason.ToString() puede devolver "ToolCalls", "Stop", etc. (PascalCase)
        // Normalizamos a snake_case para que el handler pueda comparar con "tool_calls" y "stop"
        var finishReason = completion.FinishReason == ChatFinishReason.ToolCalls ? "tool_calls"
                         : completion.FinishReason == ChatFinishReason.Stop      ? "stop"
                         : completion.FinishReason.ToString().ToLowerInvariant();

        IReadOnlyList<Core.Interfaces.ChatToolCall>? toolCalls = null;

        if (completion.ToolCalls is { Count: > 0 })
        {
            toolCalls = completion.ToolCalls
                .Select(tc => new Core.Interfaces.ChatToolCall(
                    tc.Id,
                    tc.FunctionName,
                    tc.FunctionArguments.ToString()))
                .ToList();
        }

        return new ChatCompletionResponse(
            Role: "assistant",
            Content: content,
            ToolCalls: toolCalls,
            FinishReason: finishReason);
    }
}
