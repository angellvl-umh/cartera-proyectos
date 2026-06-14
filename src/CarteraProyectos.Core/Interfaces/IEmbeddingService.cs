namespace CarteraProyectos.Core.Interfaces;

public interface IEmbeddingService
{
    Task<float[]> GetEmbeddingAsync(string text, CancellationToken ct = default);
}
