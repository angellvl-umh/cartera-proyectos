using System.Globalization;
using System.Text;

namespace CarteraProyectos.Core.Common;

/// <summary>
/// Helper para normalizar texto antes de comparaciones de búsqueda.
/// Pliega mayúsculas/minúsculas y diacríticos (acentos) para que "promocion"
/// encuentre "Promoción". La ñ no es un diacrítico separable por FormD, por lo
/// que se conserva tal cual (NORMALIZE("ñ") != NORMALIZE("n")).
/// </summary>
public static class TextSearchNormalizer
{
    /// <summary>
    /// Normaliza el texto para búsqueda: descompone Unicode (FormD), elimina
    /// marcas diacríticas no espaciadoras y convierte a mayúsculas invariantes.
    /// </summary>
    /// <param name="text">Texto a normalizar. Si es null devuelve <see cref="string.Empty"/>.</param>
    /// <returns>Texto normalizado, listo para comparación con <see cref="StringComparison.Ordinal"/>.</returns>
    public static string Normalize(string? text)
    {
        if (text is null) return string.Empty;

        var decomposed = text.Normalize(NormalizationForm.FormD);

        var sb = new StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        return sb.ToString().ToUpperInvariant();
    }
}
