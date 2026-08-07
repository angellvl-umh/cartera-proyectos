using System.Text.Json;
using CarteraProyectos.Core.Features.Agent;
using CarteraProyectos.Core.Features.Projects.WeeklyUpdates;
using CarteraProyectos.Core.Features.Reports;
using CarteraProyectos.Core.Interfaces;
using MediatR;

namespace CarteraProyectos.Core.Features.Chat.Tools;

public static partial class ChatToolCatalog
{
    private static ChatToolEntry GetMyTasks() => new(
        Name: "get_my_tasks",
        Description: "Devuelve las tareas activas, backlog y completadas del usuario que realiza la consulta. Usa este endpoint cuando el usuario pregunte '¿qué tengo pendiente?', '¿en qué estoy trabajando?', '¿cuáles son mis tareas?'.",
        ParametersJson: """{"type":"object","properties":{},"required":[]}""",
        Execute: async (args, personId, sender, ct) =>
            await sender.Send(new AgentGetMyTasksQuery(personId), ct));

    private static ChatToolEntry GetProjects() => new(
        Name: "get_projects",
        Description: "Devuelve los proyectos asociados a los equipos del usuario con su estado, equipo principal (primaryTeamName) y progreso de tareas. Filtra opcionalmente por status (Stopped, PlanningWithClient, WaitingForDevelopers, PlanningSprint, InSprint, DevelopmentOutsideSprint, InTesting, Completed, PostponedByClient). Usa este endpoint cuando el usuario pregunte por sus proyectos o el estado general de la cartera.",
        ParametersJson: """{"type":"object","properties":{"status":{"type":"string","description":"Filtro por estado del proyecto (opcional)."}},"required":[]}""",
        Execute: async (args, personId, sender, ct) =>
            await sender.Send(new AgentGetProjectsQuery(personId, GetStringOrNull(args, "status")), ct));

    private static ChatToolEntry GetProjectDetail() => new(
        Name: "get_project_detail",
        Description: "Devuelve información detallada de un proyecto específico: estado, sprints activos, tareas pendientes y progreso. Usa este endpoint cuando el usuario pregunte '¿cómo va el proyecto X?' o pida información sobre un proyecto concreto.",
        ParametersJson: """{"type":"object","properties":{"id":{"type":"integer","description":"ID del proyecto."}},"required":["id"]}""",
        Execute: async (args, personId, sender, ct) =>
            await sender.Send(new AgentGetProjectDetailQuery(GetInt(args, "id")), ct));

    private static ChatToolEntry GetCapacity() => new(
        Name: "get_capacity",
        Description: "Devuelve la carga de trabajo de todos los equipos y sus miembros (Green=disponible ≤3 tareas activas, Yellow=cargado 4-6, Red=saturado ≥7). Usa este endpoint cuando el usuario pregunte qué equipo tiene más disponibilidad o capacidad para un nuevo proyecto.",
        ParametersJson: """{"type":"object","properties":{},"required":[]}""",
        Execute: async (args, personId, sender, ct) =>
            await sender.Send(new AgentGetCapacityQuery(), ct));

    private static ChatToolEntry SearchTasks() => new(
        Name: "search_tasks",
        Description: "Busca tareas cuya descripción coincida con el texto proporcionado usando similitud semántica. Usa este endpoint para identificar una tarea concreta cuando el usuario la mencione de forma natural, por ejemplo 'la tarea del proxy' o 'lo del certificado SSL'. Devuelve las 5 tareas más similares con su puntuación.",
        ParametersJson: """{"type":"object","properties":{"q":{"type":"string","description":"Texto de búsqueda."}},"required":["q"]}""",
        Execute: async (args, personId, sender, ct) =>
            await sender.Send(new AgentSearchTasksQuery(GetString(args, "q"), personId), ct));

    private static ChatToolEntry UpdateTaskStatus() => new(
        Name: "update_task_status",
        Description: "Actualiza el estado de una tarea. Estados válidos: Backlog, ToDo, InProgress, Blocked, Done, Discarded. Done y Discarded son terminales y no pueden retroceder. Pueden hacerlo el Gestor y cualquier miembro de un equipo asignado al proyecto de la tarea (no solo los asignados). Úsalo cuando el usuario diga que ha terminado una tarea, que está bloqueado, que empieza a trabajar en algo o que quiere descartar una tarea. IMPORTANTE: confirma siempre con el usuario antes de ejecutar.",
        ParametersJson: """{"type":"object","properties":{"id":{"type":"integer","description":"ID de la tarea."},"status":{"type":"string","description":"Nuevo estado: Backlog, ToDo, InProgress, Blocked, Done, Discarded."}},"required":["id","status"]}""",
        Execute: async (args, personId, sender, ct) =>
        {
            await sender.Send(new AgentUpdateTaskStatusCommand(personId, GetInt(args, "id"), GetString(args, "status")), ct);
            return new { message = $"Estado de la tarea {GetInt(args, "id")} actualizado a '{GetString(args, "status")}'." };
        });

    private static ChatToolEntry CreateTask() => new(
        Name: "create_task",
        Description: "Crea una nueva tarea en un proyecto. Requiere el ID del proyecto. La prioridad puede ser: Low, Medium, High, Critical. Si assignToSelf es true, la tarea se asigna automáticamente al usuario. Úsalo cuando el usuario quiera registrar trabajo pendiente.",
        ParametersJson: """{"type":"object","properties":{"projectId":{"type":"integer","description":"ID del proyecto."},"title":{"type":"string","description":"Título de la tarea."},"description":{"type":"string","description":"Descripción opcional."},"priority":{"type":"string","description":"Low, Medium, High, Critical (por defecto Medium)."},"epicId":{"type":"integer","description":"ID de la épica (opcional)."},"sprintId":{"type":"integer","description":"ID del sprint (opcional)."},"assignToSelf":{"type":"boolean","description":"Asignar al usuario actual (por defecto true)."}},"required":["projectId","title"]}""",
        Execute: async (args, personId, sender, ct) =>
        {
            var id = await sender.Send(new AgentCreateTaskCommand(
                personId,
                GetInt(args, "projectId"),
                GetString(args, "title"),
                GetStringOrNull(args, "description"),
                GetString(args, "priority", "Medium"),
                GetIntOrNull(args, "epicId"),
                GetIntOrNull(args, "sprintId"),
                GetBool(args, "assignToSelf", true)), ct);
            return new { id, message = $"Tarea '{GetString(args, "title")}' creada con ID {id}." };
        });

    private static ChatToolEntry AddTaskComment() => new(
        Name: "add_task_comment",
        Description: "Añade un comentario de seguimiento a una tarea existente. El comentario queda registrado con el autor y la fecha. Úsalo cuando el usuario quiera documentar avances, bloqueos o novedades sobre una tarea.",
        ParametersJson: """{"type":"object","properties":{"id":{"type":"integer","description":"ID de la tarea."},"text":{"type":"string","description":"Texto del comentario."}},"required":["id","text"]}""",
        Execute: async (args, personId, sender, ct) =>
        {
            await sender.Send(new AgentAddCommentCommand(personId, GetInt(args, "id"), GetString(args, "text")), ct);
            return new { message = $"Comentario añadido a la tarea {GetInt(args, "id")}." };
        });

    private static ChatToolEntry AddProjectNote() => new(
        Name: "add_project_note",
        Description: "Añade una nota o comentario de seguimiento a un proyecto existente. La nota queda registrada con el autor y la fecha. Úsalo cuando el usuario quiera documentar decisiones, hitos, bloqueos o novedades sobre un proyecto concreto.",
        ParametersJson: """{"type":"object","properties":{"id":{"type":"integer","description":"ID del proyecto."},"text":{"type":"string","description":"Texto de la nota."}},"required":["id","text"]}""",
        Execute: async (args, personId, sender, ct) =>
        {
            var noteId = await sender.Send(new AgentAddProjectNoteCommand(personId, GetInt(args, "id"), GetString(args, "text")), ct);
            return new { id = noteId, message = $"Nota añadida al proyecto {GetInt(args, "id")}." };
        });

    private static ChatToolEntry AddWeeklyUpdate() => new(
        Name: "add_weekly_update",
        Description: "Registra (o actualiza si ya existe uno esta semana) el avance semanal del usuario en un proyecto. healthStatus debe ser OnTrack (en curso), AtRisk (en riesgo) o Blocked (bloqueado). Solo se permite un registro por persona y proyecto por semana ISO; un segundo registro la misma semana actualiza el anterior. Úsalo cuando el usuario quiera reportar cómo va un proyecto esta semana.",
        ParametersJson: """{"type":"object","properties":{"id":{"type":"integer","description":"ID del proyecto."},"summary":{"type":"string","description":"Resumen del avance semanal."},"healthStatus":{"type":"string","description":"OnTrack, AtRisk o Blocked."}},"required":["id","summary","healthStatus"]}""",
        Execute: async (args, personId, sender, ct) =>
        {
            if (!Enum.TryParse<CarteraProyectos.Core.Domain.ProjectHealthStatus>(GetString(args, "healthStatus"), out var health))
                throw new InvalidOperationException("healthStatus debe ser OnTrack, AtRisk o Blocked.");
            var updateId = await sender.Send(new UpsertProjectWeeklyUpdateCommand(GetInt(args, "id"), personId, GetString(args, "summary"), health), ct);
            return new { id = updateId, message = $"Avance semanal registrado para el proyecto {GetInt(args, "id")}." };
        });

    private static ChatToolEntry GetWeeklyPortfolioReport() => new(
        Name: "get_weekly_portfolio_report",
        Description: "Devuelve proyectos en riesgo y otros clasificados por estado de actualización de esta semana. Filtrable por año y equipo. Usa este endpoint cuando necesites un resumen del estado de la cartera esta semana.",
        ParametersJson: """{"type":"object","properties":{"year":{"type":"integer","description":"Año de cartera (opcional)."},"teamId":{"type":"integer","description":"ID del equipo (opcional)."}},"required":[]}""",
        Execute: async (args, personId, sender, ct) =>
            await sender.Send(new GetWeeklyPortfolioReportQuery(GetIntOrNull(args, "year"), GetIntOrNull(args, "teamId")), ct));

    private static ChatToolEntry ReindexEmbeddings() => new(
        Name: "reindex_embeddings",
        Description: "Genera o actualiza los embeddings vectoriales de todas las tareas para permitir búsqueda semántica. Ejecutar después de crear o modificar tareas masivamente. Puede tardar varios segundos según el número de tareas.",
        ParametersJson: """{"type":"object","properties":{},"required":[]}""",
        Execute: async (args, personId, sender, ct) =>
            await sender.Send(new AgentReindexCommand(), ct));
}
