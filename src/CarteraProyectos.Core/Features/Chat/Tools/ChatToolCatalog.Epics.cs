using CarteraProyectos.Core.Features.Agent;
using CarteraProyectos.Core.Features.Epics;

namespace CarteraProyectos.Core.Features.Chat.Tools;

public static partial class ChatToolCatalog
{
    private static ChatToolEntry GetEpics() => new(
        Name: "get_epics",
        Description: "Lista las épicas de un proyecto ordenadas por SortOrder, con conteo de tareas totales y completadas.",
        ParametersJson: """{"type":"object","properties":{"projectId":{"type":"integer","description":"ID del proyecto."},"page":{"type":"integer","description":"Página (default 1)."},"pageSize":{"type":"integer","description":"Tamaño de página (default 20, máx 100)."}},"required":["projectId"]}""",
        Execute: async (args, personId, sender, ct) =>
            await sender.Send(
                new GetEpicsQuery(
                    GetInt(args, "projectId"),
                    GetInt(args, "page", 1),
                    GetInt(args, "pageSize", 20)),
                ct));

    private static ChatToolEntry CreateEpic() => new(
        Name: "create_epic",
        Description: "Crea una épica en un proyecto. NOTA: este endpoint no comprueba pertenencia al equipo del proyecto (hueco preexistente del endpoint REST; no se corrige aquí). IMPORTANTE: confirma siempre con el usuario antes de ejecutar.",
        ParametersJson: """{"type":"object","properties":{"projectId":{"type":"integer","description":"ID del proyecto."},"title":{"type":"string","description":"Título de la épica."},"description":{"type":"string","description":"Descripción (opcional)."},"priority":{"type":"integer","description":"Prioridad numérica."},"sortOrder":{"type":"integer","description":"Orden de visualización."},"estimationHours":{"type":"integer","description":"Estimación en horas (opcional)."},"estimationPoints":{"type":"integer","description":"Estimación en puntos (opcional)."}},"required":["projectId","title","priority","sortOrder"]}""",
        Execute: async (args, personId, sender, ct) =>
        {
            var id = await sender.Send(
                new AgentCreateEpicCommand(
                    personId,
                    GetInt(args, "projectId"),
                    GetString(args, "title"),
                    GetStringOrNull(args, "description"),
                    GetInt(args, "priority"),
                    GetInt(args, "sortOrder"),
                    GetIntOrNull(args, "estimationHours"),
                    GetIntOrNull(args, "estimationPoints")),
                ct);
            return new { id, message = $"Épica '{GetString(args, "title")}' creada con ID {id}." };
        });

    private static ChatToolEntry UpdateEpic() => new(
        Name: "update_epic",
        Description: "Actualiza una épica existente. NOTA: este endpoint no comprueba pertenencia al equipo del proyecto (hueco preexistente del endpoint REST; no se corrige aquí). IMPORTANTE: confirma siempre con el usuario antes de ejecutar.",
        ParametersJson: """{"type":"object","properties":{"id":{"type":"integer","description":"ID de la épica."},"title":{"type":"string","description":"Título de la épica."},"description":{"type":"string","description":"Descripción (opcional)."},"priority":{"type":"integer","description":"Prioridad numérica."},"sortOrder":{"type":"integer","description":"Orden de visualización."},"estimationHours":{"type":"integer","description":"Estimación en horas (opcional)."},"estimationPoints":{"type":"integer","description":"Estimación en puntos (opcional)."}},"required":["id","title","priority","sortOrder"]}""",
        Execute: async (args, personId, sender, ct) =>
        {
            await sender.Send(
                new AgentUpdateEpicCommand(
                    personId,
                    GetInt(args, "id"),
                    GetString(args, "title"),
                    GetStringOrNull(args, "description"),
                    GetInt(args, "priority"),
                    GetInt(args, "sortOrder"),
                    GetIntOrNull(args, "estimationHours"),
                    GetIntOrNull(args, "estimationPoints")),
                ct);
            return new { message = $"Épica {GetInt(args, "id")} actualizada." };
        });
}
