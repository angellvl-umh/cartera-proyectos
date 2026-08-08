namespace CarteraProyectos.Core.Interfaces;

/// <summary>
/// Resuelve la URL pública base del backend, usada para construir enlaces absolutos
/// de descarga (exports Excel, gráficos) devueltos por las tools del agente IA.
/// Core no depende de HttpContext: las tools se ejecutan desde <c>SendChatMessageHandler</c>,
/// fuera del pipeline HTTP, así que la URL sale de configuración en vez del request.
/// </summary>
public interface IPublicUrlProvider
{
    string BuildChartUrl(Guid id);
    string BuildExportUrl(Guid id);
}
