using System.Text.Json;
using CarteraProyectos.Core.Features.Agent;
using MediatR;

namespace CarteraProyectos.Core.Features.Chat.Tools;

public static partial class ChatToolCatalog
{
    private static ChatToolEntry ChartTeamCapacity() => new(
        Name: "chart_team_capacity",
        Description: "Genera un gráfico SVG de barras horizontales con la carga de trabajo actual por persona (top 15), " +
                     "coloreadas según nivel (verde=disponible, amarillo=cargado, rojo=saturado). " +
                     "Devuelve la imagen inline en markdown. " +
                     "Úsalo cuando el usuario pida ver la capacidad de los equipos visualmente.",
        ParametersJson: """{"type":"object","properties":{},"required":[]}""",
        Execute: async (args, personId, sender, ct) =>
        {
            var result = await sender.Send(new AgentChartTeamCapacityQuery(personId), ct);
            return new { url = result.Url, message = result.Message };
        });

    private static ChatToolEntry ChartProjectProgress() => new(
        Name: "chart_project_progress",
        Description: "Genera un gráfico SVG de barras horizontales con el porcentaje de tareas completadas por proyecto. " +
                     "Devuelve la imagen inline en markdown. " +
                     "Úsalo cuando el usuario pida ver el progreso de sus proyectos de forma visual.",
        ParametersJson: """{"type":"object","properties":{},"required":[]}""",
        Execute: async (args, personId, sender, ct) =>
        {
            var result = await sender.Send(new AgentChartProjectProgressQuery(personId), ct);
            return new { url = result.Url, message = result.Message };
        });

    private static ChatToolEntry ChartMyTasksByStatus() => new(
        Name: "chart_my_tasks_by_status",
        Description: "Genera un gráfico SVG con la distribución de las tareas del usuario por estado. " +
                     "chartType: 'donut' (por defecto) o 'bar'. " +
                     "Devuelve la imagen inline en markdown. " +
                     "Úsalo cuando el usuario pida un gráfico de sus tareas.",
        ParametersJson: """{"type":"object","properties":{"chartType":{"type":"string","description":"Tipo de gráfico: 'donut' (por defecto) o 'bar'."}},"required":[]}""",
        Execute: async (args, personId, sender, ct) =>
        {
            var chartType = GetString(args, "chartType", "donut");
            var result = await sender.Send(
                new AgentChartMyTasksByStatusQuery(personId, chartType), ct);
            return new { url = result.Url, message = result.Message };
        });

    private static ChatToolEntry ChartProjectsByStatus() => new(
        Name: "chart_projects_by_status",
        Description: "Genera un gráfico SVG con la distribución de proyectos por estado. " +
                     "chartType: 'pie' (por defecto) o 'bar'. Filtra opcionalmente por status. " +
                     "Devuelve la imagen inline en markdown. " +
                     "Úsalo cuando el usuario pida un gráfico de proyectos por estado.",
        ParametersJson: """{"type":"object","properties":{"chartType":{"type":"string","description":"Tipo de gráfico: 'pie' (por defecto) o 'bar'."},"status":{"type":"string","description":"Filtro por estado (opcional)."}},"required":[]}""",
        Execute: async (args, personId, sender, ct) =>
        {
            var chartType = GetString(args, "chartType", "pie");
            var status    = GetStringOrNull(args, "status");
            var result = await sender.Send(
                new AgentChartProjectsByStatusQuery(personId, status, chartType), ct);
            return new { url = result.Url, message = result.Message };
        });

    private static ChatToolEntry ChartProjectsByTeam() => new(
        Name: "chart_projects_by_team",
        Description: "Genera un gráfico SVG con la distribución de proyectos por equipo principal. " +
                     "chartType: 'bar' (por defecto) o 'pie'. " +
                     "Devuelve la imagen inline en markdown. " +
                     "Úsalo cuando el usuario pida un gráfico de proyectos por equipo.",
        ParametersJson: """{"type":"object","properties":{"chartType":{"type":"string","description":"Tipo de gráfico: 'bar' (por defecto) o 'pie'."}},"required":[]}""",
        Execute: async (args, personId, sender, ct) =>
        {
            var chartType = GetString(args, "chartType", "bar");
            var result = await sender.Send(
                new AgentChartProjectsByTeamQuery(personId, chartType), ct);
            return new { url = result.Url, message = result.Message };
        });
}
