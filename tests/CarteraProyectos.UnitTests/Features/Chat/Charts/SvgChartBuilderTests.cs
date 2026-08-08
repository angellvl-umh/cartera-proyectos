using CarteraProyectos.Core.Features.Chat.Tools.Charts;
using Shouldly;

namespace CarteraProyectos.UnitTests.Features.Chat.Charts;

public class SvgChartBuilderTests
{
    // ── BuildHorizontalBarChart ───────────────────────────────────────────────

    [Fact]
    public void BuildHorizontalBarChart_ReturnsValidSvgEnvelope()
    {
        var items = new List<(string Label, double Value, string Color)>
        {
            ("Item A", 10, "#ff0000"),
            ("Item B", 20, "#00ff00"),
            ("Item C", 5,  "#0000ff"),
        };

        var svg = SvgChartBuilder.BuildHorizontalBarChart("Título de prueba", items);

        svg.ShouldStartWith("<svg");
        svg.ShouldEndWith("</svg>");
    }

    [Fact]
    public void BuildHorizontalBarChart_ContainsOneRectPerItem()
    {
        var items = new List<(string Label, double Value, string Color)>
        {
            ("Alpha", 10, "#1890ff"),
            ("Beta",  20, "#52c41a"),
            ("Gamma", 15, "#fa8c16"),
        };

        var svg = SvgChartBuilder.BuildHorizontalBarChart("Test", items);

        // Cada item produce un <rect
        var rectCount = CountOccurrences(svg, "<rect");
        rectCount.ShouldBe(items.Count);
    }

    [Fact]
    public void BuildHorizontalBarChart_ContainsEachLabel()
    {
        var items = new List<(string Label, double Value, string Color)>
        {
            ("Portal Alumno",   10, "#1890ff"),
            ("Gestor Prácticas", 5, "#52c41a"),
        };

        var svg = SvgChartBuilder.BuildHorizontalBarChart("Test", items);

        svg.ShouldContain("Portal Alumno");
        // Acento en "Prácticas" — el label puede estar truncado pero "Pr" siempre aparece
        svg.ShouldContain("Pr");
    }

    [Fact]
    public void BuildHorizontalBarChart_SpecialCharsInLabel_AreEscaped()
    {
        // El carácter & en el label debe aparecer como &amp; en el SVG
        var items = new List<(string Label, double Value, string Color)>
        {
            ("I+D & Innovación", 8, "#1890ff"),
            ("< Test >",         3, "#52c41a"),
        };

        var svg = SvgChartBuilder.BuildHorizontalBarChart("Test", items);

        // El & debe estar escapado como &amp;
        svg.ShouldContain("&amp;");
        // El < debe estar escapado como &lt;
        svg.ShouldContain("&lt;");
    }

    [Fact]
    public void BuildHorizontalBarChart_EmptyList_ReturnsValidSvg()
    {
        var items = new List<(string Label, double Value, string Color)>();

        var svg = SvgChartBuilder.BuildHorizontalBarChart("Sin datos", items);

        svg.ShouldStartWith("<svg");
        svg.ShouldEndWith("</svg>");
        // Sin items, no hay <rect
        CountOccurrences(svg, "<rect").ShouldBe(0);
    }

    [Fact]
    public void BuildHorizontalBarChart_WithValueSuffix_SuffixAppearsInOutput()
    {
        var items = new List<(string Label, double Value, string Color)>
        {
            ("Carga", 5, "#1890ff"),
        };

        var svg = SvgChartBuilder.BuildHorizontalBarChart("Test", items, valueSuffix: " tareas");

        svg.ShouldContain("tareas");
    }

    // ── BuildPieChart ─────────────────────────────────────────────────────────

    [Fact]
    public void BuildPieChart_ReturnsValidSvgEnvelope()
    {
        var items = new List<(string Label, double Value, string Color)>
        {
            ("Estado A", 30, "#1890ff"),
            ("Estado B", 70, "#52c41a"),
        };

        var svg = SvgChartBuilder.BuildPieChart("Distribución", items, donut: false);

        svg.ShouldStartWith("<svg");
        svg.ShouldEndWith("</svg>");
    }

    [Fact]
    public void BuildPieChart_WithDonutTrue_ContainsDonutCircle()
    {
        var items = new List<(string Label, double Value, string Color)>
        {
            ("A", 40, "#1890ff"),
            ("B", 60, "#52c41a"),
        };

        var svg = SvgChartBuilder.BuildPieChart("Donut", items, donut: true);

        // El agujero del donut se dibuja como <circle
        svg.ShouldContain("<circle");
    }

    [Fact]
    public void BuildPieChart_WithDonutFalse_DoesNotContainDonutCircle()
    {
        var items = new List<(string Label, double Value, string Color)>
        {
            ("A", 40, "#1890ff"),
            ("B", 60, "#52c41a"),
        };

        var svg = SvgChartBuilder.BuildPieChart("Tarta", items, donut: false);

        // Sin donut no debe haber <circle
        svg.ShouldNotContain("<circle");
    }

    [Fact]
    public void BuildPieChart_SingleItemAt100Percent_DoesNotThrow()
    {
        // Caso borde: único item al 100% — el código maneja el path degenerado
        var items = new List<(string Label, double Value, string Color)>
        {
            ("Único estado", 100, "#1890ff"),
        };

        string svg = null!;
        Should.NotThrow(() =>
            svg = SvgChartBuilder.BuildPieChart("100%", items, donut: false));

        svg.ShouldStartWith("<svg");
        svg.ShouldEndWith("</svg>");
    }

    [Fact]
    public void BuildPieChart_EmptyList_ReturnsSvgWithSinDatos()
    {
        var items = new List<(string Label, double Value, string Color)>();

        var svg = SvgChartBuilder.BuildPieChart("Sin datos", items, donut: false);

        svg.ShouldStartWith("<svg");
        svg.ShouldEndWith("</svg>");
        svg.ShouldContain("Sin datos");
    }

    [Fact]
    public void BuildPieChart_ZeroTotalValue_ReturnsSvgWithSinDatos()
    {
        // Items con value 0 → total = 0, debe tratarse igual que lista vacía
        var items = new List<(string Label, double Value, string Color)>
        {
            ("A", 0, "#1890ff"),
            ("B", 0, "#52c41a"),
        };

        var svg = SvgChartBuilder.BuildPieChart("Sin valores", items, donut: false);

        svg.ShouldContain("Sin datos");
    }

    // ── CategoricalColor ──────────────────────────────────────────────────────

    [Fact]
    public void CategoricalColor_CyclesOverPalette()
    {
        // El color para el índice 0 debe ser el mismo que para un índice suficientemente alto
        // que cicle de vuelta a la misma posición (paleta de 9 colores)
        var color0 = SvgChartBuilder.CategoricalColor(0);
        var color9 = SvgChartBuilder.CategoricalColor(9);

        color0.ShouldBe(color9);
    }

    [Fact]
    public void CategoricalColor_ReturnsHexColor()
    {
        var color = SvgChartBuilder.CategoricalColor(0);

        color.ShouldStartWith("#");
        color.Length.ShouldBe(7); // #RRGGBB
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private static int CountOccurrences(string source, string pattern)
    {
        int count = 0;
        int index = 0;
        while ((index = source.IndexOf(pattern, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += pattern.Length;
        }

        return count;
    }
}
