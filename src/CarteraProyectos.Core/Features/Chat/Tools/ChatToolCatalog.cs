using System.Text.Json;
using CarteraProyectos.Core.Interfaces;
using MediatR;

namespace CarteraProyectos.Core.Features.Chat.Tools;

/// <summary>
/// Entrada del catálogo de tools del asistente.
/// Cada entrada describe una tool (nombre, descripción, JSON Schema de parámetros)
/// y contiene el delegado que la ejecuta, recibiendo personId de forma explícita
/// (nunca desde los argumentos del modelo).
/// </summary>
public sealed record ChatToolEntry(
    string Name,
    string Description,
    string ParametersJson,
    Func<JsonElement, int, ISender, CancellationToken, Task<object?>> Execute
);

/// <summary>
/// Catálogo de todas las tools disponibles para el asistente de chat.
/// </summary>
public static partial class ChatToolCatalog
{
    /// <summary>
    /// Devuelve la lista completa de tools disponibles.
    /// </summary>
    public static IReadOnlyList<ChatToolEntry> All() =>
    [
        GetMyTasks(),
        GetProjects(),
        GetProjectDetail(),
        GetCapacity(),
        SearchTasks(),
        UpdateTaskStatus(),
        CreateTask(),
        AddTaskComment(),
        AddProjectNote(),
        AddWeeklyUpdate(),
        GetWeeklyPortfolioReport(),
        GetPersons(),
        CreatePerson(),
        UpdatePerson(),
        SetPersonActive(),
        UpdateProjectStatus(),
        CreateProject(),
        UpdateProject(),
        GetProjectRisks(),
        AddProjectRisk(),
        UpdateProjectRisk(),
        GetProjectDependencies(),
        AddProjectDependency(),
        ReindexEmbeddings(),
    ];

    /// <summary>
    /// Obtiene las definiciones de tool para enviarlas al LLM.
    /// </summary>
    public static IReadOnlyList<ChatToolDefinition> GetDefinitions() =>
        All().Select(e => new ChatToolDefinition(e.Name, e.Description, e.ParametersJson)).ToList();

    /// <summary>
    /// Ejecuta una tool por nombre, devolviendo el resultado serializado como string JSON.
    /// Lanza <see cref="KeyNotFoundException"/> si la tool no existe.
    /// </summary>
    public static async Task<string> ExecuteAsync(
        string toolName, string argumentsJson, int personId,
        ISender sender, CancellationToken ct)
    {
        var entry = All().FirstOrDefault(e => e.Name == toolName)
            ?? throw new KeyNotFoundException($"Tool '{toolName}' no encontrada en el catálogo.");

        JsonElement args;
        try
        {
            args = JsonDocument.Parse(argumentsJson).RootElement;
        }
        catch
        {
            args = JsonDocument.Parse("{}").RootElement;
        }

        var result = await entry.Execute(args, personId, sender, ct);
        return JsonSerializer.Serialize(result, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    internal static string GetString(JsonElement el, string key, string defaultValue = "")
        => el.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString() ?? defaultValue : defaultValue;

    internal static string? GetStringOrNull(JsonElement el, string key)
        => el.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString() : null;

    internal static int GetInt(JsonElement el, string key, int defaultValue = 0)
        => el.TryGetProperty(key, out var v) && v.TryGetInt32(out var i) ? i : defaultValue;

    internal static int? GetIntOrNull(JsonElement el, string key)
        => el.TryGetProperty(key, out var v) && v.TryGetInt32(out var i) ? i : null;

    internal static bool GetBool(JsonElement el, string key, bool defaultValue = false)
        => el.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.True ? true
         : el.TryGetProperty(key, out var v2) && v2.ValueKind == JsonValueKind.False ? false
         : defaultValue;

    internal static bool? GetBoolOrNull(JsonElement el, string key)
        => el.TryGetProperty(key, out var v)
            ? v.ValueKind == JsonValueKind.True ? true
            : v.ValueKind == JsonValueKind.False ? false
            : null
          : null;

    internal static decimal? GetDecimalOrNull(JsonElement el, string key)
        => el.TryGetProperty(key, out var v) && v.TryGetDecimal(out var d) ? d : null;

    internal static DateOnly? GetDateOrNull(JsonElement el, string key)
        => el.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.String
           && DateOnly.TryParse(v.GetString(), out var d) ? d : null;
}
