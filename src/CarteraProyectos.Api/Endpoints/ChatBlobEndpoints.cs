using CarteraProyectos.Core.Interfaces;

namespace CarteraProyectos.Api.Endpoints;

/// <summary>
/// Endpoints para servir blobs efímeros (gráficos SVG y exports Excel) generados
/// por las tools del agente IA. Son capability-URLs: no requieren autenticación porque
/// el propio chat (autenticado) genera y entrega el Guid al usuario, y el Guid no es
/// adivinable. El blob expira automáticamente a los 20 minutos de inactividad.
///
/// La URL absoluta de descarga se construye en <see cref="CarteraProyectos.Core.Interfaces.IPublicUrlProvider"/>
/// (Core) a partir de <c>Chat:PublicBaseUrl</c>, no desde HttpContext: las tools que generan
/// el link corren dentro de <c>SendChatMessageHandler</c>, fuera del pipeline HTTP.
/// </summary>
public static class ChatBlobEndpoints
{
    public static IEndpointRouteBuilder MapChatBlobEndpoints(this IEndpointRouteBuilder app)
    {
        // GET /api/chat/charts/{id} — sirve un gráfico SVG generado por el agente IA
        app.MapGet("/api/chat/charts/{id:guid}", (Guid id, IEphemeralBlobStore blobStore) =>
        {
            var blob = blobStore.TryGet(id);
            if (blob is null) return Results.NotFound();

            return Results.Bytes(blob.Data, blob.ContentType);
        })
        .WithName("GetChatChart")
        .WithTags("Chat")
        .WithDescription(
            "Sirve un gráfico SVG generado por el agente IA. Capability-URL con Guid no adivinable. " +
            "No requiere autenticación. Expira a los 20 minutos de inactividad.");

        // GET /api/chat/exports/{id} — sirve un export Excel generado por el agente IA
        app.MapGet("/api/chat/exports/{id:guid}", (Guid id, IEphemeralBlobStore blobStore) =>
        {
            var blob = blobStore.TryGet(id);
            if (blob is null) return Results.NotFound();

            // Incluir Content-Disposition con el nombre de fichero para que el navegador
            // proponga un nombre de archivo correcto al descargar.
            var fileName = blob.FileName ?? $"export-{id}.xlsx";
            return Results.File(
                blob.Data,
                blob.ContentType,
                fileDownloadName: fileName);
        })
        .WithName("GetChatExport")
        .WithTags("Chat")
        .WithDescription(
            "Sirve un export Excel generado por el agente IA. Capability-URL con Guid no adivinable. " +
            "No requiere autenticación. Expira a los 20 minutos de inactividad. " +
            "La respuesta incluye Content-Disposition con el nombre de fichero original.");

        return app;
    }
}
