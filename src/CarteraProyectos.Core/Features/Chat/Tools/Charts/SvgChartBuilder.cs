using System.Text;

namespace CarteraProyectos.Core.Features.Chat.Tools.Charts;

/// <summary>
/// Helper estático puro (sin DI) para construir gráficos SVG a mano.
/// No usa ninguna librería de gráficos externa; genera el XML directamente.
/// </summary>
public static class SvgChartBuilder
{
    // Paleta categórica — cicla por índice si hay más categorías que colores
    private static readonly string[] CategoricalPalette =
    [
        "#1890ff", "#52c41a", "#fa8c16", "#722ed1", "#13c2c2",
        "#ff4d4f", "#2f54eb", "#d4b106", "#595959",
    ];

    /// <summary>
    /// Devuelve un color categórico para el índice dado (cicla sobre la paleta).
    /// </summary>
    public static string CategoricalColor(int index)
        => CategoricalPalette[index % CategoricalPalette.Length];

    // ──────────────────────────────────────────────────────────────────────────
    // Barras horizontales
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Construye un gráfico de barras horizontales SVG.
    /// Cada item tiene etiqueta, valor y color de barra.
    /// </summary>
    public static string BuildHorizontalBarChart(
        string title,
        IReadOnlyList<(string Label, double Value, string Color)> items,
        string? valueSuffix = null)
    {
        const int svgWidth      = 620;
        const int titleHeight   = 36;
        const int barHeight     = 24;
        const int barGap        = 10;
        const int labelWidth    = 160;  // ancho reservado para etiquetas
        const int barAreaWidth  = 360;  // ancho máximo para las barras
        const int valueColWidth = 50;   // ancho para el valor numérico a la derecha
        const int paddingLeft   = 16;
        const int paddingTop    = 10;

        double maxValue = items.Count == 0 ? 1 : items.Max(i => i.Value);
        if (maxValue <= 0) maxValue = 1;

        int svgHeight = titleHeight + paddingTop * 2
                      + items.Count * (barHeight + barGap);

        var sb = new StringBuilder();
        sb.Append($"""<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {svgWidth} {svgHeight}" style="font-family:sans-serif;background:#fff">""");

        // Título
        sb.Append($"""<text x="{svgWidth / 2}" y="{paddingTop + 20}" text-anchor="middle" font-size="15" font-weight="bold" fill="#222">{Esc(title)}</text>""");

        int y = titleHeight + paddingTop;
        foreach (var item in items)
        {
            double barWidth = barAreaWidth * (item.Value / maxValue);

            // Etiqueta
            sb.Append($"""<text x="{paddingLeft}" y="{y + barHeight - 6}" font-size="12" fill="#444" text-anchor="start">{Esc(TruncateLabel(item.Label, 22))}</text>""");

            // Barra
            int barX = paddingLeft + labelWidth;
            sb.Append($"""<rect x="{barX}" y="{y}" width="{barWidth:F1}" height="{barHeight}" fill="{item.Color}" rx="3"/>""");

            // Valor numérico al final de la barra
            string valueText = FormatValue(item.Value) + (valueSuffix ?? string.Empty);
            sb.Append($"""<text x="{barX + barWidth + 6}" y="{y + barHeight - 6}" font-size="11" fill="#555">{Esc(valueText)}</text>""");

            y += barHeight + barGap;
        }

        sb.Append("</svg>");
        return sb.ToString();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Tarta / donut
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Construye un gráfico de tarta (o donut) SVG con leyenda lateral.
    /// </summary>
    public static string BuildPieChart(
        string title,
        IReadOnlyList<(string Label, double Value, string Color)> items,
        bool donut = false)
    {
        const int svgWidth     = 560;
        const int svgHeight    = 320;
        const int cx           = 160;   // centro X del círculo
        const int cy           = 170;   // centro Y del círculo
        const int radius       = 120;
        const int donutRadius  = 60;
        const int legendX      = 310;
        const int legendStartY = 70;
        const int legendRowH   = 22;

        double total = items.Sum(i => i.Value);

        var sb = new StringBuilder();
        sb.Append($"""<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {svgWidth} {svgHeight}" style="font-family:sans-serif;background:#fff">""");

        // Título
        sb.Append($"""<text x="{svgWidth / 2}" y="22" text-anchor="middle" font-size="15" font-weight="bold" fill="#222">{Esc(title)}</text>""");

        if (total <= 0 || items.Count == 0)
        {
            sb.Append($"""<text x="{cx}" y="{cy}" text-anchor="middle" font-size="13" fill="#888">Sin datos</text>""");
            sb.Append("</svg>");
            return sb.ToString();
        }

        // Arcos de la tarta
        double startAngle = -Math.PI / 2; // Empezar desde las 12 en punto

        foreach (var item in items)
        {
            if (item.Value <= 0) continue;

            double sweepAngle = 2 * Math.PI * (item.Value / total);
            double endAngle   = startAngle + sweepAngle;

            double x1 = cx + radius * Math.Cos(startAngle);
            double y1 = cy + radius * Math.Sin(startAngle);
            double x2 = cx + radius * Math.Cos(endAngle);
            double y2 = cy + radius * Math.Sin(endAngle);

            int largeArcFlag = sweepAngle > Math.PI ? 1 : 0;

            // Para sectores del 100%, evitar x1==x2 (path cerrado que no se dibuja)
            if (items.Count == 1 || sweepAngle >= 2 * Math.PI - 0.001)
            {
                // Dibuja dos semiarcos
                sb.Append($"""<path d="M {cx},{cy - radius} A {radius},{radius} 0 0,1 {cx},{cy + radius} A {radius},{radius} 0 0,1 {cx},{cy - radius} Z" fill="{item.Color}"/>""");
            }
            else
            {
                sb.Append($"""<path d="M {cx:F2},{cy:F2} L {x1:F2},{y1:F2} A {radius},{radius} 0 {largeArcFlag},1 {x2:F2},{y2:F2} Z" fill="{item.Color}"/>""");
            }

            startAngle = endAngle;
        }

        // Agujero del donut (círculo blanco encima)
        if (donut)
            sb.Append($"""<circle cx="{cx}" cy="{cy}" r="{donutRadius}" fill="#fff"/>""");

        // Leyenda
        for (int i = 0; i < items.Count; i++)
        {
            int ly = legendStartY + i * legendRowH;
            double pct = total > 0 ? items[i].Value / total * 100 : 0;
            string label = $"{TruncateLabel(items[i].Label, 20)} ({FormatValue(items[i].Value)}, {pct:F1}%)";

            sb.Append($"""<rect x="{legendX}" y="{ly}" width="14" height="14" fill="{items[i].Color}" rx="2"/>""");
            sb.Append($"""<text x="{legendX + 20}" y="{ly + 12}" font-size="12" fill="#333">{Esc(label)}</text>""");
        }

        sb.Append("</svg>");
        return sb.ToString();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Utilidades privadas
    // ──────────────────────────────────────────────────────────────────────────

    private static string Esc(string s)
        => s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");

    private static string TruncateLabel(string label, int maxLen)
        => label.Length <= maxLen ? label : label[..maxLen] + "…";

    private static string FormatValue(double v)
        => v == Math.Floor(v) ? ((int)v).ToString() : v.ToString("F1");
}
