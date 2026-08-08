using System.Text;
using CarteraProyectos.Core.Features.Chat.Tools.Charts;
using CarteraProyectos.Core.Interfaces;
using MediatR;

namespace CarteraProyectos.Core.Features.Agent;

// ─── Queries ─────────────────────────────────────────────────────────────────

/// <summary>Genera un gráfico SVG de barras horizontales con la carga por persona (top 15).</summary>
public record AgentChartTeamCapacityQuery(int PersonId) : IRequest<AgentChartResult>;

/// <summary>Genera un gráfico SVG de barras con el progreso (completadas vs pendientes) por proyecto.</summary>
public record AgentChartProjectProgressQuery(int PersonId) : IRequest<AgentChartResult>;

/// <summary>Genera un gráfico SVG (donut o barras) con la distribución de las tareas del usuario por estado.</summary>
public record AgentChartMyTasksByStatusQuery(int PersonId, string ChartType) : IRequest<AgentChartResult>;

/// <summary>Genera un gráfico SVG (tarta o barras) con la distribución de proyectos por estado.</summary>
public record AgentChartProjectsByStatusQuery(int PersonId, string? Status, string ChartType) : IRequest<AgentChartResult>;

/// <summary>Genera un gráfico SVG (barras o tarta) con la distribución de proyectos por equipo principal.</summary>
public record AgentChartProjectsByTeamQuery(int PersonId, string ChartType) : IRequest<AgentChartResult>;

/// <summary>
/// DTO de resultado para las tools de gráficos.
/// Message ya incluye el link formateado como imagen markdown si Url no es null.
/// </summary>
public record AgentChartResult(string? Url, string Message);

// ─── Helpers compartidos ─────────────────────────────────────────────────────

file static class ChartHelpers
{
    internal static (byte[] Bytes, string ContentType) SvgToBytes(string svg)
        => (Encoding.UTF8.GetBytes(svg), "image/svg+xml");

    internal static string MarkdownImage(string url)
        => $"![gráfico]({url})";
}

// ─── Handlers ────────────────────────────────────────────────────────────────

public sealed class AgentChartTeamCapacityHandler(
    ISender sender,
    IEphemeralBlobStore blobStore,
    IPublicUrlProvider urlProvider)
    : IRequestHandler<AgentChartTeamCapacityQuery, AgentChartResult>
{
    public async Task<AgentChartResult> Handle(
        AgentChartTeamCapacityQuery request, CancellationToken ct)
    {
        var capacity = await sender.Send(new AgentGetCapacityQuery(), ct);

        // Aplanar todos los miembros de todos los equipos
        var members = capacity.Teams
            .SelectMany(t => t.Members.Select(m => (
                Label: $"{m.Name} ({t.TeamName})",
                Value: (double)m.ActiveTasks,
                Color: m.LoadLevel switch
                {
                    "Red"    => "#f44336",
                    "Yellow" => "#ff9800",
                    _        => "#4caf50",
                })))
            .OrderByDescending(x => x.Value)
            .Take(15)
            .ToList();

        if (members.Count == 0)
        {
            return new AgentChartResult(
                Url: null,
                Message: "No hay datos de capacidad disponibles.");
        }

        var svg = SvgChartBuilder.BuildHorizontalBarChart(
            "Carga de trabajo por persona (tareas activas)",
            members,
            valueSuffix: " tareas");

        var (bytes, contentType) = ChartHelpers.SvgToBytes(svg);
        var id  = blobStore.Store(bytes, contentType, null);
        var url = urlProvider.BuildChartUrl(id);

        return new AgentChartResult(url, ChartHelpers.MarkdownImage(url));
    }
}

public sealed class AgentChartProjectProgressHandler(
    ISender sender,
    IEphemeralBlobStore blobStore,
    IPublicUrlProvider urlProvider)
    : IRequestHandler<AgentChartProjectProgressQuery, AgentChartResult>
{
    public async Task<AgentChartResult> Handle(
        AgentChartProjectProgressQuery request, CancellationToken ct)
    {
        var projects = await sender.Send(
            new AgentGetProjectsQuery(request.PersonId, null), ct);

        if (projects.Count == 0)
        {
            return new AgentChartResult(
                Url: null,
                Message: "No hay proyectos disponibles para generar el gráfico.");
        }

        // Barras de tareas completadas (verde) — una barra por proyecto
        // Podríamos hacer barras apiladas con dos colores, pero al ser SVG a mano
        // preferimos mostrar el % completado como una única barra proporcional al total,
        // y al lado las tareas hechas sobre el total en el sufijo.
        var items = projects.Select(p => (
            Label: p.Title,
            Value: p.TotalTasks > 0 ? (double)p.DoneTasks / p.TotalTasks * 100 : 0.0,
            Color: "#52c41a"))
            .ToList();

        var svg = SvgChartBuilder.BuildHorizontalBarChart(
            "Progreso de proyectos (% tareas completadas)",
            items,
            valueSuffix: "%");

        var (bytes, contentType) = ChartHelpers.SvgToBytes(svg);
        var id  = blobStore.Store(bytes, contentType, null);
        var url = urlProvider.BuildChartUrl(id);

        return new AgentChartResult(url, ChartHelpers.MarkdownImage(url));
    }
}

public sealed class AgentChartMyTasksByStatusHandler(
    ISender sender,
    IEphemeralBlobStore blobStore,
    IPublicUrlProvider urlProvider)
    : IRequestHandler<AgentChartMyTasksByStatusQuery, AgentChartResult>
{
    // Colores fijos por estado
    private static readonly Dictionary<string, string> StatusColors = new(StringComparer.OrdinalIgnoreCase)
    {
        ["InProgress"] = "#2196f3",
        ["ToDo"]       = "#ff9800",
        ["Backlog"]    = "#9e9e9e",
        ["Blocked"]    = "#f44336",
        ["Done"]       = "#4caf50",
    };

    public async Task<AgentChartResult> Handle(
        AgentChartMyTasksByStatusQuery request, CancellationToken ct)
    {
        var myTasks = await sender.Send(new AgentGetMyTasksQuery(request.PersonId), ct);

        // Agregar por estado
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        // ActiveTasks — agrupar por Status real
        foreach (var t in myTasks.ActiveTasks)
            counts[t.Status] = counts.GetValueOrDefault(t.Status) + 1;

        // BacklogTasks — todas en estado Backlog
        counts["Backlog"] = counts.GetValueOrDefault("Backlog") + myTasks.BacklogTasks.Count;

        // DoneTasks (contador)
        if (myTasks.DoneTasks > 0)
            counts["Done"] = myTasks.DoneTasks;

        if (counts.Count == 0 || counts.Values.Sum() == 0)
        {
            return new AgentChartResult(
                Url: null,
                Message: "No tienes tareas asignadas para mostrar en el gráfico.");
        }

        var items = counts
            .Where(kv => kv.Value > 0)
            .Select((kv, idx) => (
                Label: kv.Key,
                Value: (double)kv.Value,
                Color: StatusColors.GetValueOrDefault(kv.Key, SvgChartBuilder.CategoricalColor(idx))))
            .ToList();

        bool donut = !string.Equals(request.ChartType, "bar", StringComparison.OrdinalIgnoreCase);

        string svg = donut
            ? SvgChartBuilder.BuildPieChart("Mis tareas por estado", items, donut: true)
            : SvgChartBuilder.BuildHorizontalBarChart("Mis tareas por estado", items);

        var (bytes, contentType) = ChartHelpers.SvgToBytes(svg);
        var id  = blobStore.Store(bytes, contentType, null);
        var url = urlProvider.BuildChartUrl(id);

        return new AgentChartResult(url, ChartHelpers.MarkdownImage(url));
    }
}

public sealed class AgentChartProjectsByStatusHandler(
    ISender sender,
    IEphemeralBlobStore blobStore,
    IPublicUrlProvider urlProvider)
    : IRequestHandler<AgentChartProjectsByStatusQuery, AgentChartResult>
{
    public async Task<AgentChartResult> Handle(
        AgentChartProjectsByStatusQuery request, CancellationToken ct)
    {
        var projects = await sender.Send(
            new AgentGetProjectsQuery(request.PersonId, request.Status), ct);

        if (projects.Count == 0)
        {
            return new AgentChartResult(
                Url: null,
                Message: "No hay proyectos disponibles para generar el gráfico.");
        }

        var grouped = projects
            .GroupBy(p => p.Status)
            .Select((g, idx) => (
                Label: g.Key,
                Value: (double)g.Count(),
                Color: SvgChartBuilder.CategoricalColor(idx)))
            .ToList();

        bool pie = !string.Equals(request.ChartType, "bar", StringComparison.OrdinalIgnoreCase);

        string svg = pie
            ? SvgChartBuilder.BuildPieChart("Proyectos por estado", grouped, donut: false)
            : SvgChartBuilder.BuildHorizontalBarChart("Proyectos por estado", grouped);

        var (bytes, contentType) = ChartHelpers.SvgToBytes(svg);
        var id  = blobStore.Store(bytes, contentType, null);
        var url = urlProvider.BuildChartUrl(id);

        return new AgentChartResult(url, ChartHelpers.MarkdownImage(url));
    }
}

public sealed class AgentChartProjectsByTeamHandler(
    ISender sender,
    IEphemeralBlobStore blobStore,
    IPublicUrlProvider urlProvider)
    : IRequestHandler<AgentChartProjectsByTeamQuery, AgentChartResult>
{
    public async Task<AgentChartResult> Handle(
        AgentChartProjectsByTeamQuery request, CancellationToken ct)
    {
        var projects = await sender.Send(
            new AgentGetProjectsQuery(request.PersonId, null), ct);

        if (projects.Count == 0)
        {
            return new AgentChartResult(
                Url: null,
                Message: "No hay proyectos disponibles para generar el gráfico.");
        }

        var grouped = projects
            .GroupBy(p => p.PrimaryTeamName ?? "Sin equipo")
            .Select((g, idx) => (
                Label: g.Key,
                Value: (double)g.Count(),
                Color: SvgChartBuilder.CategoricalColor(idx)))
            .OrderByDescending(x => x.Value)
            .ToList();

        bool usePie = string.Equals(request.ChartType, "pie", StringComparison.OrdinalIgnoreCase);

        string svg = usePie
            ? SvgChartBuilder.BuildPieChart("Proyectos por equipo", grouped, donut: false)
            : SvgChartBuilder.BuildHorizontalBarChart("Proyectos por equipo", grouped);

        var (bytes, contentType) = ChartHelpers.SvgToBytes(svg);
        var id  = blobStore.Store(bytes, contentType, null);
        var url = urlProvider.BuildChartUrl(id);

        return new AgentChartResult(url, ChartHelpers.MarkdownImage(url));
    }
}
