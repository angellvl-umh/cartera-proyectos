using CarteraProyectos.Core.Features.Chat;
using CarteraProyectos.Core.Interfaces;
using MediatR;

namespace CarteraProyectos.Api.Endpoints;

public static class ChatEndpoints
{
    public static IEndpointRouteBuilder MapChatEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/chat")
            .WithTags("Chat")
            .RequireAuthorization();

        // POST /api/chat/conversations — crear conversación
        group.MapPost("/conversations", async (
            CreateConversationRequest req,
            HttpContext ctx,
            IAppDbContext db,
            ISender sender,
            CancellationToken ct) =>
        {
            var person = await CurrentUser.ResolveAsync(ctx, db, ct);
            if (person is null) return Results.Unauthorized();

            var id = await sender.Send(new CreateConversationCommand(person.Id, req.Title), ct);
            return Results.Created($"/api/chat/conversations/{id}", new { id });
        })
        .WithName("CreateConversation")
        .WithDescription("Crea una nueva conversación de chat para el usuario autenticado.");

        // GET /api/chat/conversations — listar conversaciones del usuario
        group.MapGet("/conversations", async (
            HttpContext ctx,
            IAppDbContext db,
            ISender sender,
            CancellationToken ct,
            int page = 1, int pageSize = 20) =>
        {
            var person = await CurrentUser.ResolveAsync(ctx, db, ct);
            if (person is null) return Results.Unauthorized();

            var result = await sender.Send(new GetConversationsQuery(person.Id, page, pageSize), ct);
            return Results.Ok(result);
        })
        .WithName("GetConversations")
        .WithDescription("Lista las conversaciones del usuario autenticado, ordenadas por actividad reciente. Soporta paginación.");

        // GET /api/chat/conversations/{id}/messages — mensajes de una conversación
        group.MapGet("/conversations/{id:int}/messages", async (
            int id,
            HttpContext ctx,
            IAppDbContext db,
            ISender sender,
            CancellationToken ct) =>
        {
            var person = await CurrentUser.ResolveAsync(ctx, db, ct);
            if (person is null) return Results.Unauthorized();

            try
            {
                var messages = await sender.Send(new GetConversationMessagesQuery(person.Id, id), ct);
                return Results.Ok(messages);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
        })
        .WithName("GetConversationMessages")
        .WithDescription("Devuelve los mensajes de una conversación del usuario autenticado, en orden cronológico.");

        // POST /api/chat/conversations/{id}/messages — enviar mensaje y recibir respuesta del asistente
        group.MapPost("/conversations/{id:int}/messages", async (
            int id,
            SendMessageRequest req,
            HttpContext ctx,
            IAppDbContext db,
            ISender sender,
            CancellationToken ct) =>
        {
            var person = await CurrentUser.ResolveAsync(ctx, db, ct);
            if (person is null) return Results.Unauthorized();

            try
            {
                var result = await sender.Send(new SendChatMessageCommand(person.Id, id, req.Text), ct);
                return Results.Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
        })
        .WithName("SendChatMessage")
        .WithDescription("Envía un mensaje a una conversación y devuelve la respuesta del asistente (con posible invocación de tools). La respuesta incluye el texto del asistente y si se alcanzó el límite de iteraciones de razonamiento.");

        // DELETE /api/chat/conversations/{id} — eliminar conversación
        group.MapDelete("/conversations/{id:int}", async (
            int id,
            HttpContext ctx,
            IAppDbContext db,
            ISender sender,
            CancellationToken ct) =>
        {
            var person = await CurrentUser.ResolveAsync(ctx, db, ct);
            if (person is null) return Results.Unauthorized();

            try
            {
                await sender.Send(new DeleteConversationCommand(person.Id, id), ct);
                return Results.NoContent();
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
        })
        .WithName("DeleteConversation")
        .WithDescription("Elimina una conversación del usuario autenticado y todos sus mensajes.");

        return app;
    }
}

record CreateConversationRequest(string Title);
record SendMessageRequest(string Text);
