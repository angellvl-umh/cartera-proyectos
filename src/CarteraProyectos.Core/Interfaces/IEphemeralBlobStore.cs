namespace CarteraProyectos.Core.Interfaces;

/// <summary>
/// Blob efímero que agrupa los datos de un archivo temporal generado por el agente IA.
/// </summary>
public record EphemeralBlob(byte[] Data, string ContentType, string? FileName);

/// <summary>
/// Almacén temporal de blobs en memoria con expiración deslizante.
/// Usado por las tools del agente IA para servir exports Excel y gráficos SVG
/// a través de capability-URLs de un solo uso (Guid no adivinable).
/// </summary>
public interface IEphemeralBlobStore
{
    /// <summary>
    /// Guarda un blob y devuelve el Guid que lo identifica.
    /// El blob expirará automáticamente tras un período de inactividad (SlidingExpiration).
    /// </summary>
    Guid Store(byte[] data, string contentType, string? fileName);

    /// <summary>
    /// Intenta recuperar un blob por su identificador.
    /// Devuelve <c>null</c> si el blob no existe o ya expiró.
    /// </summary>
    EphemeralBlob? TryGet(Guid id);
}
