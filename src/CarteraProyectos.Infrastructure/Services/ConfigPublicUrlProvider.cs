using CarteraProyectos.Core.Interfaces;
using Microsoft.Extensions.Configuration;

namespace CarteraProyectos.Infrastructure.Services;

/// <summary>
/// Implementación de <see cref="IPublicUrlProvider"/> basada en <c>Chat:PublicBaseUrl</c>.
/// En Docker se alimenta de la misma variable <c>PUBLIC_URL</c> que ya usa Cors:Origins
/// (docker-compose.yml); en desarrollo local cae al puerto por defecto del backend.
/// </summary>
public sealed class ConfigPublicUrlProvider(IConfiguration configuration) : IPublicUrlProvider
{
    private string BaseUrl => (configuration["Chat:PublicBaseUrl"] ?? "http://localhost:5000").TrimEnd('/');

    public string BuildChartUrl(Guid id) => $"{BaseUrl}/api/chat/charts/{id}";

    public string BuildExportUrl(Guid id) => $"{BaseUrl}/api/chat/exports/{id}";
}
