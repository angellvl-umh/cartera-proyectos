using CarteraProyectos.Core.Features.Teams;

namespace CarteraProyectos.Core.Features.Chat.Tools;

public static partial class ChatToolCatalog
{
    private static ChatToolEntry GetTeams() => new(
        Name: "get_teams",
        Description: "Lista todos los equipos con nombre, descripción, jefe de equipo y número de miembros.",
        ParametersJson: """{"type":"object","properties":{"page":{"type":"integer","description":"Página (default 1)."},"pageSize":{"type":"integer","description":"Tamaño de página (default 20, máx 100)."}},"required":[]}""",
        Execute: async (args, personId, sender, ct) =>
            await sender.Send(
                new GetTeamsQuery(
                    GetInt(args, "page", 1),
                    GetInt(args, "pageSize", 20)),
                ct));

    private static ChatToolEntry GetTeamActivity() => new(
        Name: "get_team_activity",
        Description: "Actividad actual de todos los equipos: en qué tarea está trabajando cada persona (tareas InProgress o Blocked). Las personas sin tareas activas aparecen como disponibles.",
        ParametersJson: """{"type":"object","properties":{},"required":[]}""",
        Execute: async (args, personId, sender, ct) =>
            await sender.Send(new GetTeamActivityQuery(), ct));
}
