using CarteraProyectos.Core.Features.Agent;

namespace CarteraProyectos.Core.Features.Chat.Tools;

public static partial class ChatToolCatalog
{
    private static ChatToolEntry ReorderBacklogItem() => new(
        Name: "reorder_backlog_item",
        Description: "Reordena las tareas de un proyecto. Recibe una lista ordenada de IDs y reasigna SortOrder en múltiplos de 10. NOTA: no comprueba pertenencia al equipo del proyecto (hueco preexistente del endpoint REST). IMPORTANTE: confirma siempre con el usuario antes de ejecutar.",
        ParametersJson: """{"type":"object","properties":{"projectId":{"type":"integer","description":"ID del proyecto."},"orderedIds":{"type":"array","items":{"type":"integer"},"description":"IDs de las tareas en el orden deseado."}},"required":["projectId","orderedIds"]}""",
        Execute: async (args, personId, sender, ct) =>
        {
            var projectId = GetInt(args, "projectId");
            var orderedIds = args.TryGetProperty("orderedIds", out var idsEl)
                ? idsEl.EnumerateArray().Select(e => e.GetInt32()).ToList()
                : new List<int>();

            await sender.Send(
                new AgentReorderBacklogCommand(personId, projectId, orderedIds),
                ct);
            return new { message = $"Reordenadas {orderedIds.Count} tareas en el proyecto {projectId}." };
        });

    private static ChatToolEntry BulkAssignToSprint() => new(
        Name: "bulk_assign_to_sprint",
        Description: "Asigna masivamente tareas a un sprint (o al backlog si sprintId es null). Todos los IDs deben pertenecer al proyecto. IMPORTANTE: confirma siempre con el usuario antes de ejecutar.",
        ParametersJson: """{"type":"object","properties":{"projectId":{"type":"integer","description":"ID del proyecto."},"workItemIds":{"type":"array","items":{"type":"integer"},"description":"IDs de las tareas a asignar."},"sprintId":{"type":"integer","description":"ID del sprint destino. Omitir o null para mover al backlog."}},"required":["projectId","workItemIds"]}""",
        Execute: async (args, personId, sender, ct) =>
        {
            var projectId = GetInt(args, "projectId");
            var workItemIds = args.TryGetProperty("workItemIds", out var idsEl)
                ? idsEl.EnumerateArray().Select(e => e.GetInt32()).ToList()
                : new List<int>();
            var sprintId = GetIntOrNull(args, "sprintId");

            await sender.Send(
                new AgentBulkAssignToSprintCommand(personId, projectId, workItemIds, sprintId),
                ct);
            var destination = sprintId.HasValue ? $"sprint {sprintId}" : "backlog";
            return new { message = $"{workItemIds.Count} tareas asignadas al {destination} en el proyecto {projectId}." };
        });
}
