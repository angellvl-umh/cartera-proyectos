using CarteraProyectos.Core.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace CarteraProyectos.Infrastructure.Services;

/// <summary>
/// Implementación de <see cref="IEphemeralBlobStore"/> sobre <see cref="IMemoryCache"/>.
/// Cada blob se almacena con SlidingExpiration de 20 minutos: si nadie lo descarga
/// en ese plazo desaparece automáticamente sin necesidad de limpieza explícita.
/// </summary>
public sealed class MemoryCacheBlobStore(IMemoryCache cache) : IEphemeralBlobStore
{
    private static readonly TimeSpan SlidingExpiration = TimeSpan.FromMinutes(20);

    public Guid Store(byte[] data, string contentType, string? fileName)
    {
        var id = Guid.NewGuid();
        var blob = new EphemeralBlob(data, contentType, fileName);
        cache.Set(CacheKey(id), blob, new MemoryCacheEntryOptions
        {
            SlidingExpiration = SlidingExpiration,
        });
        return id;
    }

    public EphemeralBlob? TryGet(Guid id)
        => cache.TryGetValue(CacheKey(id), out EphemeralBlob? blob) ? blob : null;

    private static string CacheKey(Guid id) => $"ephemeral-blob:{id}";
}
