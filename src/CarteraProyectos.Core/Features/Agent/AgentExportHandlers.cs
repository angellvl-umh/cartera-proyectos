using CarteraProyectos.Core.Features.Chat.Tools.Exports;
using CarteraProyectos.Core.Features.Reports;
using CarteraProyectos.Core.Interfaces;
using MediatR;

namespace CarteraProyectos.Core.Features.Agent;

// ─── Queries ─────────────────────────────────────────────────────────────────

/// <summary>
/// Exporta la lista de proyectos visibles para el usuario a un Excel.
/// Devuelve la URL de descarga del fichero efímero.
/// </summary>
public record AgentExportProjectsExcelQuery(int PersonId, string? Status)
    : IRequest<AgentExportResult>;

/// <summary>
/// Exporta el informe semanal de cartera a un Excel.
/// Devuelve la URL de descarga del fichero efímero.
/// </summary>
public record AgentExportWeeklyReportExcelQuery(int PersonId, int? Year, int? TeamId)
    : IRequest<AgentExportResult>;

/// <summary>
/// DTO de resultado para las tools de exportación.
/// Si Url es null, no se generó fichero (lista vacía u otro motivo).
/// </summary>
public record AgentExportResult(string? Url, string Message);

// ─── Handlers ────────────────────────────────────────────────────────────────

public sealed class AgentExportProjectsExcelHandler(
    IProjectsReadService projectsService,
    IEphemeralBlobStore blobStore,
    IPublicUrlProvider urlProvider)
    : IRequestHandler<AgentExportProjectsExcelQuery, AgentExportResult>
{
    private const string ContentType =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public async Task<AgentExportResult> Handle(
        AgentExportProjectsExcelQuery request, CancellationToken ct)
    {
        var projects = await projectsService.GetAsync(request.PersonId, request.Status, ct);

        if (projects.Count == 0)
            return new AgentExportResult(
                Url: null,
                Message: "No se encontraron proyectos con los filtros indicados. No se ha generado ningún fichero.");

        var bytes = ExcelExportBuilder.BuildProjectsWorkbook(projects);
        var id = blobStore.Store(bytes, ContentType, "proyectos.xlsx");
        var url = urlProvider.BuildExportUrl(id);

        return new AgentExportResult(
            Url: url,
            Message: $"Export generado con {projects.Count} proyecto(s). Descárgalo aquí: {url}");
    }
}

public sealed class AgentExportWeeklyReportExcelHandler(
    IWeeklyPortfolioReportService reportService,
    IEphemeralBlobStore blobStore,
    IPublicUrlProvider urlProvider)
    : IRequestHandler<AgentExportWeeklyReportExcelQuery, AgentExportResult>
{
    private const string ContentType =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public async Task<AgentExportResult> Handle(
        AgentExportWeeklyReportExcelQuery request, CancellationToken ct)
    {
        var report = await reportService.GetAsync(
            new GetWeeklyPortfolioReportQuery(request.Year, request.TeamId), ct);

        var total = report.AtRiskProjects.Count + report.OtherProjects.Count;
        if (total == 0)
            return new AgentExportResult(
                Url: null,
                Message: "No hay proyectos activos en el informe semanal para los filtros indicados. No se ha generado ningún fichero.");

        var bytes = ExcelExportBuilder.BuildWeeklyReportWorkbook(report);
        var id = blobStore.Store(bytes, ContentType, "informe-semanal-cartera.xlsx");
        var url = urlProvider.BuildExportUrl(id);

        return new AgentExportResult(
            Url: url,
            Message: $"Informe semanal exportado con {total} proyecto(s). Descárgalo aquí: {url}");
    }
}
