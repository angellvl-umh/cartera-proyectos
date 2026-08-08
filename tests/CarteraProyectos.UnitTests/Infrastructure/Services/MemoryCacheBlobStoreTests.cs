using CarteraProyectos.Core.Interfaces;
using CarteraProyectos.Infrastructure.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Shouldly;

namespace CarteraProyectos.UnitTests.Infrastructure.Services;

public class MemoryCacheBlobStoreTests
{
    private static MemoryCacheBlobStore CreateStore()
    {
        var cache = new MemoryCache(Options.Create(new MemoryCacheOptions()));
        return new MemoryCacheBlobStore(cache);
    }

    [Fact]
    public void Store_ThenTryGet_ReturnsSameBlob()
    {
        var store = CreateStore();
        var data = "contenido de prueba"u8.ToArray();

        var id = store.Store(data, "text/plain", "archivo.txt");
        var blob = store.TryGet(id);

        blob.ShouldNotBeNull();
        blob!.Data.ShouldBe(data);
        blob.ContentType.ShouldBe("text/plain");
        blob.FileName.ShouldBe("archivo.txt");
    }

    [Fact]
    public void TryGet_UnknownGuid_ReturnsNull()
    {
        var store = CreateStore();

        var blob = store.TryGet(Guid.NewGuid());

        blob.ShouldBeNull();
    }

    [Fact]
    public void Store_MultipleBlobs_EachRetrievableByItsOwnId()
    {
        var store = CreateStore();
        var data1 = "blob uno"u8.ToArray();
        var data2 = "blob dos"u8.ToArray();

        var id1 = store.Store(data1, "text/plain", "uno.txt");
        var id2 = store.Store(data2, "application/json", "dos.json");

        id1.ShouldNotBe(id2);

        store.TryGet(id1)!.Data.ShouldBe(data1);
        store.TryGet(id2)!.ContentType.ShouldBe("application/json");
    }

    [Fact]
    public void Store_WithNullFileName_PreservesNull()
    {
        var store = CreateStore();

        var id = store.Store([0x01, 0x02], "image/svg+xml", null);
        var blob = store.TryGet(id);

        blob.ShouldNotBeNull();
        blob!.FileName.ShouldBeNull();
    }

    [Fact]
    public void Store_ReturnsNewGuidEachTime()
    {
        var store = CreateStore();
        var data = new byte[] { 1 };

        var id1 = store.Store(data, "a", null);
        var id2 = store.Store(data, "a", null);

        id1.ShouldNotBe(id2);
    }
}
