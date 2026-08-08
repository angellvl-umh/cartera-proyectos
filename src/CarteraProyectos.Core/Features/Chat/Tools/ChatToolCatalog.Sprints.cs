using CarteraProyectos.Core.Features.Agent;
using CarteraProyectos.Core.Features.Reports;
using CarteraProyectos.Core.Features.Sprints;

namespace CarteraProyectos.Core.Features.Chat.Tools;

public static partial class ChatToolCatalog
{
    private static ChatToolEntry GetSprints() => new(
        Name: "get_sprints",
        Description: "Lista los sprints de un proyecto paginados, ordenados por fecha de inicio descendente. Incluye estado, capacidad, puntos comprometidos y entregados.",
        ParametersJson: """{"type":"object","properties":{"projectId":{"type":"integer","description":"ID del proyecto."},"page":{"type":"integer","description":"Página (default 1)."},"pageSize":{"type":"integer","description":"Tamaño de página (default 20, máx 100)."}},"required":["projectId"]}""",
        Execute: async (args, personId, sender, ct) =>
            await sender.Send(
                new GetSprintsQuery(
                    GetInt(args, "projectId"),
                    GetInt(args, "page", 1),
                    GetInt(args, "pageSize", 20)),
                ct));

    private static ChatToolEntry GetSprintBurndown() => new(
        Name: "get_sprint_burndown",
        Description: "Burndown chart del sprint: puntos ideales y reales restantes por día. Requiere que el sprint tenga fechas de inicio y fin definidas; si no las tiene, el servidor devuelve un error. Los días futuros tienen RemainingPoints null.",
        ParametersJson: """{"type":"object","properties":{"projectId":{"type":"integer","description":"ID del proyecto."},"sprintId":{"type":"integer","description":"ID del sprint."}},"required":["projectId","sprintId"]}""",
        Execute: async (args, personId, sender, ct) =>
            await sender.Send(
                new GetSprintBurndownQuery(
                    GetInt(args, "projectId"),
                    GetInt(args, "sprintId")),
                ct));

    private static ChatToolEntry CreateSprint() => new(
        Name: "create_sprint",
        Description: "Crea un nuevo sprint en estado Planning para un proyecto. Fechas en formato yyyy-MM-dd. IMPORTANTE: confirma siempre con el usuario antes de ejecutar.",
        ParametersJson: """{"type":"object","properties":{"projectId":{"type":"integer","description":"ID del proyecto."},"name":{"type":"string","description":"Nombre del sprint."},"goal":{"type":"string","description":"Objetivo del sprint (opcional)."},"startDate":{"type":"string","description":"Fecha de inicio yyyy-MM-dd (opcional)."},"endDate":{"type":"string","description":"Fecha de fin yyyy-MM-dd (opcional)."},"capacity":{"type":"integer","description":"Capacidad en puntos (opcional)."}},"required":["projectId","name"]}""",
        Execute: async (args, personId, sender, ct) =>
        {
            var id = await sender.Send(
                new AgentCreateSprintCommand(
                    personId,
                    GetInt(args, "projectId"),
                    GetString(args, "name"),
                    GetStringOrNull(args, "goal"),
                    GetDateOrNull(args, "startDate"),
                    GetDateOrNull(args, "endDate"),
                    GetIntOrNull(args, "capacity")),
                ct);
            return new { id, message = $"Sprint '{GetString(args, "name")}' creado con ID {id}." };
        });

    private static ChatToolEntry ActivateSprint() => new(
        Name: "activate_sprint",
        Description: "Activa un sprint (Planning → Active). Solo puede haber un sprint activo por proyecto. IMPORTANTE: confirma siempre con el usuario antes de ejecutar.",
        ParametersJson: """{"type":"object","properties":{"id":{"type":"integer","description":"ID del sprint a activar."}},"required":["id"]}""",
        Execute: async (args, personId, sender, ct) =>
        {
            await sender.Send(
                new AgentActivateSprintCommand(personId, GetInt(args, "id")),
                ct);
            return new { message = $"Sprint {GetInt(args, "id")} activado." };
        });

    private static ChatToolEntry CompleteSprint() => new(
        Name: "complete_sprint",
        Description: "Completa un sprint (Active → Completed). Si hay tareas sin terminar, es obligatorio indicar carryOver: 'Backlog' (mueve al backlog) o 'Sprint' con targetSprintId (mueve a otro sprint en Planning del mismo proyecto). IMPORTANTE: confirma siempre con el usuario antes de ejecutar.",
        ParametersJson: """{"type":"object","properties":{"id":{"type":"integer","description":"ID del sprint a completar."},"carryOver":{"type":"string","description":"Destino de las tareas sin terminar: 'Backlog' o 'Sprint'. Obligatorio si hay tareas sin terminar."},"targetSprintId":{"type":"integer","description":"ID del sprint destino para el carry-over. Obligatorio si carryOver es 'Sprint'."}},"required":["id"]}""",
        Execute: async (args, personId, sender, ct) =>
        {
            await sender.Send(
                new AgentCompleteSprintCommand(
                    personId,
                    GetInt(args, "id"),
                    GetStringOrNull(args, "carryOver"),
                    GetIntOrNull(args, "targetSprintId")),
                ct);
            return new { message = $"Sprint {GetInt(args, "id")} completado." };
        });
}
