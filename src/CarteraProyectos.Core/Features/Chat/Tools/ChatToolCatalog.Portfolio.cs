using CarteraProyectos.Core.Features.Reports;

namespace CarteraProyectos.Core.Features.Chat.Tools;

public static partial class ChatToolCatalog
{
    private static ChatToolEntry GetPortfolioRoadmap() => new(
        Name: "get_portfolio_roadmap",
        Description: "Roadmap visual de la cartera agrupado por equipo primario para el año indicado (por defecto el año actual). Devuelve proyectos con fechas, hitos y estado. Los proyectos sin StartDate aparecen en 'undated'; los sin equipo asignado en 'unassigned'.",
        ParametersJson: """{"type":"object","properties":{"year":{"type":"integer","description":"Año del roadmap (optional; default año actual)."}},"required":[]}""",
        Execute: async (args, personId, sender, ct) =>
            await sender.Send(
                new GetPortfolioRoadmapQuery(GetIntOrNull(args, "year")),
                ct));

    private static ChatToolEntry GetCapacityForecast() => new(
        Name: "get_capacity_forecast",
        Description: "Previsión de carga de trabajo por equipo y trimestre para el año indicado (por defecto el año actual). Nivel de carga: Green <70 %, Yellow 70–100 %, Red >100 %. La demanda se estima a partir de la complejidad y las fechas de los proyectos activos.",
        ParametersJson: """{"type":"object","properties":{"year":{"type":"integer","description":"Año de la previsión (optional; default año actual)."}},"required":[]}""",
        Execute: async (args, personId, sender, ct) =>
            await sender.Send(
                new GetCapacityForecastQuery(GetIntOrNull(args, "year")),
                ct));

    private static ChatToolEntry GetProjectVelocity() => new(
        Name: "get_project_velocity",
        Description: "Velocidad del equipo por proyecto: puntos comprometidos y entregados por cada sprint completado, con la media de velocidad.",
        ParametersJson: """{"type":"object","properties":{"projectId":{"type":"integer","description":"ID del proyecto."}},"required":["projectId"]}""",
        Execute: async (args, personId, sender, ct) =>
            await sender.Send(
                new GetProjectVelocityQuery(GetInt(args, "projectId")),
                ct));

    private static ChatToolEntry GetProjectCycleTime() => new(
        Name: "get_project_cycle_time",
        Description: "Métricas de cycle time y lead time del proyecto: tiempo desde inicio de trabajo hasta Done (cycle time) y desde creación hasta Done (lead time), para las tareas completadas.",
        ParametersJson: """{"type":"object","properties":{"projectId":{"type":"integer","description":"ID del proyecto."}},"required":["projectId"]}""",
        Execute: async (args, personId, sender, ct) =>
            await sender.Send(
                new GetProjectCycleTimeQuery(GetInt(args, "projectId")),
                ct));
}
