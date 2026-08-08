using CarteraProyectos.Core.Features.Agent;
using CarteraProyectos.Core.Features.Reports;
using ClosedXML.Excel;

namespace CarteraProyectos.Core.Features.Chat.Tools.Exports;

/// <summary>
/// Helper estático puro (sin DI) para construir workbooks Excel con ClosedXML.
/// Devuelve siempre byte[] listos para guardar en IEphemeralBlobStore.
/// </summary>
public static class ExcelExportBuilder
{
    /// <summary>
    /// Construye un workbook con un listado de proyectos.
    /// Columnas: ID, Título, Estado, Unidad solicitante, Equipo principal,
    ///           Tareas totales, Tareas hechas, Sprints activos.
    /// </summary>
    public static byte[] BuildProjectsWorkbook(IReadOnlyList<AgentProjectSummaryDto> projects)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Proyectos");

        // Cabecera
        ws.Cell(1, 1).Value = "ID";
        ws.Cell(1, 2).Value = "Título";
        ws.Cell(1, 3).Value = "Estado";
        ws.Cell(1, 4).Value = "Unidad solicitante";
        ws.Cell(1, 5).Value = "Equipo principal";
        ws.Cell(1, 6).Value = "Tareas totales";
        ws.Cell(1, 7).Value = "Tareas hechas";
        ws.Cell(1, 8).Value = "Sprints activos";

        ws.Row(1).Style.Font.Bold = true;

        // Datos
        for (int i = 0; i < projects.Count; i++)
        {
            var p = projects[i];
            int row = i + 2;
            ws.Cell(row, 1).Value = p.Id;
            ws.Cell(row, 2).Value = p.Title;
            ws.Cell(row, 3).Value = p.Status;
            ws.Cell(row, 4).Value = p.RequestingUnit ?? string.Empty;
            ws.Cell(row, 5).Value = p.PrimaryTeamName ?? string.Empty;
            ws.Cell(row, 6).Value = p.TotalTasks;
            ws.Cell(row, 7).Value = p.DoneTasks;
            ws.Cell(row, 8).Value = p.ActiveSprints;
        }

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    /// <summary>
    /// Construye un workbook con el informe semanal de cartera.
    /// Columnas: En riesgo, Proyecto, Equipo principal, Estado, Estado de salud,
    ///           Última actualización esta semana, Autor, Fecha, Resumen.
    /// Las filas en riesgo van primero; luego el resto.
    /// </summary>
    public static byte[] BuildWeeklyReportWorkbook(WeeklyPortfolioReportDto report)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Informe semanal");

        // Cabecera
        ws.Cell(1, 1).Value = "En riesgo";
        ws.Cell(1, 2).Value = "Proyecto";
        ws.Cell(1, 3).Value = "Equipo principal";
        ws.Cell(1, 4).Value = "Estado";
        ws.Cell(1, 5).Value = "Estado de salud";
        ws.Cell(1, 6).Value = "Última actualización";
        ws.Cell(1, 7).Value = "Autor";
        ws.Cell(1, 8).Value = "Fecha";
        ws.Cell(1, 9).Value = "Resumen";

        ws.Row(1).Style.Font.Bold = true;

        // Datos: primero AtRiskProjects, luego OtherProjects
        var allProjects = report.AtRiskProjects.Concat(report.OtherProjects).ToList();
        for (int i = 0; i < allProjects.Count; i++)
        {
            var p = allProjects[i];
            int row = i + 2;
            ws.Cell(row, 1).Value = p.IsAtRisk ? "Sí" : "No";
            ws.Cell(row, 2).Value = p.Title;
            ws.Cell(row, 3).Value = p.PrimaryTeamName ?? string.Empty;
            ws.Cell(row, 4).Value = p.Status;
            ws.Cell(row, 5).Value = p.LatestHealthStatus ?? string.Empty;
            ws.Cell(row, 6).Value = p.HasUpdateThisWeek ? "Sí" : "No";
            ws.Cell(row, 7).Value = p.LatestAuthorName ?? string.Empty;
            ws.Cell(row, 8).Value = p.LatestUpdateDate ?? string.Empty;
            ws.Cell(row, 9).Value = p.LatestSummary ?? string.Empty;
        }

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }
}
