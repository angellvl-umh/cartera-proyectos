using CarteraProyectos.Core.Common;
using Shouldly;

namespace CarteraProyectos.UnitTests.Common;

public class TextSearchNormalizerTests
{
    // ── Null / vacío ──────────────────────────────────────────────────────────

    [Fact]
    public void Normalize_Null_ReturnsEmpty()
        => TextSearchNormalizer.Normalize(null).ShouldBe(string.Empty);

    [Fact]
    public void Normalize_Empty_ReturnsEmpty()
        => TextSearchNormalizer.Normalize("").ShouldBe(string.Empty);

    // ── Acentos vocálicos → se pliegan ────────────────────────────────────────

    [Theory]
    [InlineData("á", "A")]
    [InlineData("é", "E")]
    [InlineData("í", "I")]
    [InlineData("ó", "O")]
    [InlineData("ú", "U")]
    [InlineData("Á", "A")]
    [InlineData("É", "E")]
    [InlineData("Í", "I")]
    [InlineData("Ó", "O")]
    [InlineData("Ú", "U")]
    public void Normalize_AccentedVowel_YieldsBaseUppercase(string input, string expected)
        => TextSearchNormalizer.Normalize(input).ShouldBe(expected);

    // ── Mayúsculas/minúsculas → se pliegan ────────────────────────────────────

    [Fact]
    public void Normalize_LowercaseAndUppercase_AreEqual()
        => TextSearchNormalizer.Normalize("promocion")
            .ShouldBe(TextSearchNormalizer.Normalize("PROMOCION"));

    [Fact]
    public void Normalize_MixedCase_AreEqual()
        => TextSearchNormalizer.Normalize("Promoción")
            .ShouldBe(TextSearchNormalizer.Normalize("promocion"));

    // ── ñ en .NET: se pliega a N ──────────────────────────────────────────────
    // Nota: Aunque en español la ñ no es lingüísticamente un diacrítico de n,
    // Unicode NFD sí descompone U+00F1 (ñ) como n + U+0303 (combining tilde),
    // y .NET lo trata igual — el combining tilde se elimina como NonSpacingMark.
    // Resultado: buscar "nino" encontrará "niño". Semántica elegida: simplicidad.

    [Fact]
    public void Normalize_Enne_YieldsUpperN()
        => TextSearchNormalizer.Normalize("ñ").ShouldBe("N");

    [Fact]
    public void Normalize_Enne_EqualsN_DotNetDecomposesIt()
    {
        // NFD descompone ñ (U+00F1) en n + combining tilde (U+0303), que se descarta.
        TextSearchNormalizer.Normalize("ñ").ShouldBe(TextSearchNormalizer.Normalize("n"));
    }

    // ── Ejemplos de casos reales de búsqueda ─────────────────────────────────

    [Fact]
    public void Normalize_SearchExample_PromocionMatchesPromocion()
    {
        var candidate = TextSearchNormalizer.Normalize("Vicerrectorado de Promoción");
        var query     = TextSearchNormalizer.Normalize("promocion");

        candidate.Contains(query, StringComparison.Ordinal).ShouldBeTrue();
    }

    [Fact]
    public void Normalize_SearchExample_NinoMatchesNinyo()
    {
        // En .NET, "nino" sí coincide con "niño" porque ñ → N tras normalización.
        var candidate = TextSearchNormalizer.Normalize("Niño");
        var query     = TextSearchNormalizer.Normalize("nino");

        candidate.Contains(query, StringComparison.Ordinal).ShouldBeTrue();
    }
}
