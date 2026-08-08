using System.Text.Json;
using CarteraProyectos.Core.Features.Agent;
using MediatR;

namespace CarteraProyectos.Core.Features.Chat.Tools;

public static partial class ChatToolCatalog
{
    private static ChatToolEntry ExportProjectsExcel() => new(
        Name: "export_projects_excel",
        Description: "Genera un fichero Excel (.xlsx) con la lista de proyectos visibles para el usuario. " +
                     "Filtra opcionalmente por status. Devuelve un enlace de descarga temporal (expira en 20 minutos). " +
                     "Úsalo cuando el usuario pida exportar o descargar los proyectos.",
        ParametersJson: """{"type":"object","properties":{"status":{"type":"string","description":"Filtro por estado del proyecto (opcional): Stopped, PlanningWithClient, WaitingForDevelopers, PlanningSprint, InSprint, DevelopmentOutsideSprint, InTesting, Completed, PostponedByClient."}},"required":[]}""",
        Execute: async (args, personId, sender, ct) =>
        {
            var result = await sender.Send(
                new AgentExportProjectsExcelQuery(personId, GetStringOrNull(args, "status")), ct);

            return result.Url is not null
                ? new { url = result.Url, message = result.Message }
                : new { url = (string?)null, message = result.Message };
        });

    private static ChatToolEntry ExportWeeklyPortfolioReportExcel() => new(
        Name: "export_weekly_portfolio_report_excel",
        Description: "Genera un fichero Excel (.xlsx) con el informe semanal de seguimiento de cartera. " +
                     "Incluye proyectos en riesgo y el resto, con su estado de salud, última actualización y autor. " +
                     "Filtra opcionalmente por año y equipo. Devuelve un enlace de descarga temporal (expira en 20 minutos). " +
                     "Úsalo cuando el usuario pida exportar o descargar el informe semanal.",
        ParametersJson: """{"type":"object","properties":{"year":{"type":"integer","description":"Año de cartera (opcional)."},"teamId":{"type":"integer","description":"ID del equipo (opcional)."}},"required":[]}""",
        Execute: async (args, personId, sender, ct) =>
        {
            var result = await sender.Send(
                new AgentExportWeeklyReportExcelQuery(
                    personId,
                    GetIntOrNull(args, "year"),
                    GetIntOrNull(args, "teamId")), ct);

            return result.Url is not null
                ? new { url = result.Url, message = result.Message }
                : new { url = (string?)null, message = result.Message };
        });
}
