using CarteraProyectos.Core.Features.Agent;
using CarteraProyectos.Core.Features.Chat.Tools.Exports;
using CarteraProyectos.Core.Features.Reports;
using ClosedXML.Excel;
using Shouldly;

namespace CarteraProyectos.UnitTests.Features.Chat.Exports;

public class ExcelExportBuilderTests
{
    // ── BuildProjectsWorkbook ─────────────────────────────────────────────────

    [Fact]
    public void BuildProjectsWorkbook_WithProjects_HeaderRowIsBold()
    {
        var projects = new List<AgentProjectSummaryDto>
        {
            new(1, "Portal Alumno", "InSprint", "TIC", "Equipo Alpha", 10, 4, 1),
            new(2, "Gestor Prácticas", "Stopped", "RRHH", null, 5, 5, 0),
        };

        var bytes = ExcelExportBuilder.BuildProjectsWorkbook(projects);

        using var wb = new XLWorkbook(new MemoryStream(bytes));
        var ws = wb.Worksheets.First();

        ws.Row(1).Style.Font.Bold.ShouldBeTrue();
    }

    [Fact]
    public void BuildProjectsWorkbook_WithProjects_HeaderCellsHaveExpectedValues()
    {
        var projects = new List<AgentProjectSummaryDto>
        {
            new(1, "Portal Alumno", "InSprint", "TIC", "Equipo Alpha", 10, 4, 1),
        };

        var bytes = ExcelExportBuilder.BuildProjectsWorkbook(projects);

        using var wb = new XLWorkbook(new MemoryStream(bytes));
        var ws = wb.Worksheets.First();

        ws.Cell(1, 1).GetString().ShouldBe("ID");
        ws.Cell(1, 2).GetString().ShouldBe("Título");
        ws.Cell(1, 3).GetString().ShouldBe("Estado");
        ws.Cell(1, 4).GetString().ShouldBe("Unidad solicitante");
        ws.Cell(1, 5).GetString().ShouldBe("Equipo principal");
        ws.Cell(1, 6).GetString().ShouldBe("Tareas totales");
        ws.Cell(1, 7).GetString().ShouldBe("Tareas hechas");
        ws.Cell(1, 8).GetString().ShouldBe("Sprints activos");
    }

    [Fact]
    public void BuildProjectsWorkbook_WithTwoProjects_DataRowsMatchInput()
    {
        var projects = new List<AgentProjectSummaryDto>
        {
            new(1, "Portal Alumno", "InSprint", "TIC", "Equipo Alpha", 10, 4, 1),
            new(2, "Gestor Prácticas", "Stopped", "RRHH", null, 5, 5, 0),
        };

        var bytes = ExcelExportBuilder.BuildProjectsWorkbook(projects);

        using var wb = new XLWorkbook(new MemoryStream(bytes));
        var ws = wb.Worksheets.First();

        // Fila 2 — primer proyecto
        ws.Cell(2, 1).GetDouble().ShouldBe(1);
        ws.Cell(2, 2).GetString().ShouldBe("Portal Alumno");
        ws.Cell(2, 3).GetString().ShouldBe("InSprint");
        ws.Cell(2, 4).GetString().ShouldBe("TIC");
        ws.Cell(2, 5).GetString().ShouldBe("Equipo Alpha");
        ws.Cell(2, 6).GetDouble().ShouldBe(10);
        ws.Cell(2, 7).GetDouble().ShouldBe(4);
        ws.Cell(2, 8).GetDouble().ShouldBe(1);

        // Fila 3 — segundo proyecto (equipo null → cadena vacía)
        ws.Cell(3, 2).GetString().ShouldBe("Gestor Prácticas");
        ws.Cell(3, 5).GetString().ShouldBe(string.Empty);
    }

    [Fact]
    public void BuildProjectsWorkbook_EmptyList_ReturnsValidWorkbookWithOnlyHeader()
    {
        var bytes = ExcelExportBuilder.BuildProjectsWorkbook(new List<AgentProjectSummaryDto>());

        bytes.ShouldNotBeEmpty();
        using var wb = new XLWorkbook(new MemoryStream(bytes));
        var ws = wb.Worksheets.First();

        // Solo la fila de cabecera, la fila 2 está vacía
        ws.Cell(1, 1).GetString().ShouldBe("ID");
        ws.Cell(2, 1).IsEmpty().ShouldBeTrue();
    }

    // ── BuildWeeklyReportWorkbook ─────────────────────────────────────────────

    private static WeeklyPortfolioProjectDto MakeProjectDto(
        int id, string title, bool isAtRisk,
        string? healthStatus = null, bool hasUpdateThisWeek = true)
        => new(id, title, "Equipo Test", "InSprint",
            isAtRisk, "Resumen de prueba", healthStatus ?? (isAtRisk ? "AtRisk" : "OnTrack"),
            "Autor Test", "2026-07-01", hasUpdateThisWeek);

    [Fact]
    public void BuildWeeklyReportWorkbook_HeaderRowIsBold()
    {
        var report = new WeeklyPortfolioReportDto(
            AtRiskProjects: [MakeProjectDto(1, "Riesgo", isAtRisk: true)],
            OtherProjects:  [MakeProjectDto(2, "OK",     isAtRisk: false)]);

        var bytes = ExcelExportBuilder.BuildWeeklyReportWorkbook(report);

        using var wb = new XLWorkbook(new MemoryStream(bytes));
        var ws = wb.Worksheets.First();

        ws.Row(1).Style.Font.Bold.ShouldBeTrue();
    }

    [Fact]
    public void BuildWeeklyReportWorkbook_AtRiskProjectsAppearBeforeOtherProjects()
    {
        var report = new WeeklyPortfolioReportDto(
            AtRiskProjects: [MakeProjectDto(1, "Proyecto En Riesgo", isAtRisk: true)],
            OtherProjects:  [MakeProjectDto(2, "Proyecto OK",         isAtRisk: false)]);

        var bytes = ExcelExportBuilder.BuildWeeklyReportWorkbook(report);

        using var wb = new XLWorkbook(new MemoryStream(bytes));
        var ws = wb.Worksheets.First();

        // Fila 2 = primer AtRisk, Fila 3 = OtherProject
        ws.Cell(2, 2).GetString().ShouldBe("Proyecto En Riesgo");
        ws.Cell(3, 2).GetString().ShouldBe("Proyecto OK");
    }

    [Fact]
    public void BuildWeeklyReportWorkbook_AtRiskColumn_HasSiParaEnRiesgoYNoParaOtros()
    {
        var report = new WeeklyPortfolioReportDto(
            AtRiskProjects: [MakeProjectDto(1, "En Riesgo", isAtRisk: true)],
            OtherProjects:  [MakeProjectDto(2, "Normal",    isAtRisk: false)]);

        var bytes = ExcelExportBuilder.BuildWeeklyReportWorkbook(report);

        using var wb = new XLWorkbook(new MemoryStream(bytes));
        var ws = wb.Worksheets.First();

        ws.Cell(2, 1).GetString().ShouldBe("Sí");
        ws.Cell(3, 1).GetString().ShouldBe("No");
    }

    [Fact]
    public void BuildWeeklyReportWorkbook_MultipleAtRisk_AllAppearFirst()
    {
        var report = new WeeklyPortfolioReportDto(
            AtRiskProjects:
            [
                MakeProjectDto(1, "Riesgo A", isAtRisk: true),
                MakeProjectDto(2, "Riesgo B", isAtRisk: true),
            ],
            OtherProjects:
            [
                MakeProjectDto(3, "OK C", isAtRisk: false),
            ]);

        var bytes = ExcelExportBuilder.BuildWeeklyReportWorkbook(report);

        using var wb = new XLWorkbook(new MemoryStream(bytes));
        var ws = wb.Worksheets.First();

        ws.Cell(2, 1).GetString().ShouldBe("Sí");
        ws.Cell(3, 1).GetString().ShouldBe("Sí");
        ws.Cell(4, 1).GetString().ShouldBe("No");
        ws.Cell(4, 2).GetString().ShouldBe("OK C");
    }
}
