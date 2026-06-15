using CarteraProyectos.Core.Features.Agent;
using CarteraProyectos.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Api.Endpoints;

public static class AgentEndpoints
{
    public static void MapAgentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/agent")
            .WithTags("Agent")
            .WithGroupName("agent")
            .AddEndpointFilter(AgentApiKeyFilter);

        // ── HU-IA-07: Mis tareas ──────────────────────────────────────────────
        group.MapGet("/me", async (HttpContext http, IAppDbContext db, ISender sender) =>
        {
            var person = await ResolvePersonAsync(http, db);
            if (person is null) return Results.Problem("Usuario no encontrado. Regístrate en la plataforma primero.", statusCode: 404);
            var result = await sender.Send(new AgentGetMyTasksQuery(person.Id));
            return Results.Ok(result);
        })
        .WithName("get_my_tasks")
        .WithSummary("Obtener mis tareas pendientes")
        .WithDescription("Devuelve las tareas activas, backlog y completadas del usuario que realiza la consulta. Usa este endpoint cuando el usuario pregunte '¿qué tengo pendiente?', '¿en qué estoy trabajando?', '¿cuáles son mis tareas?'.");

        // ── HU-IA-01: Lista de proyectos ──────────────────────────────────────
        group.MapGet("/projects", async (HttpContext http, IAppDbContext db, ISender sender) =>
        {
            var person = await ResolvePersonAsync(http, db);
            if (person is null) return Results.Problem("Usuario no encontrado.", statusCode: 404);
            var result = await sender.Send(new AgentGetProjectsQuery(person.Id));
            return Results.Ok(result);
        })
        .WithName("get_projects")
        .WithSummary("Listar proyectos del usuario")
        .WithDescription("Devuelve los proyectos asociados a los equipos del usuario con su estado y progreso de tareas. Usa este endpoint cuando el usuario pregunte por sus proyectos o por el estado general de la cartera.");

        // ── HU-IA-01: Detalle de proyecto ─────────────────────────────────────
        group.MapGet("/projects/{id:int}", async (int id, ISender sender) =>
        {
            var result = await sender.Send(new AgentGetProjectDetailQuery(id));
            return Results.Ok(result);
        })
        .WithName("get_project_detail")
        .WithSummary("Obtener detalle de un proyecto")
        .WithDescription("Devuelve información detallada de un proyecto específico: estado, sprints activos, tareas pendientes y progreso. Usa este endpoint cuando el usuario pregunte '¿cómo va el proyecto X?' o pida información sobre un proyecto concreto.");

        // ── HU-IA-02: Capacidad de equipos ───────────────────────────────────
        group.MapGet("/capacity", async (ISender sender) =>
        {
            var result = await sender.Send(new AgentGetCapacityQuery());
            return Results.Ok(result);
        })
        .WithName("get_capacity")
        .WithSummary("Consultar carga y disponibilidad de equipos")
        .WithDescription("Devuelve la carga de trabajo de todos los equipos y sus miembros (Green=disponible ≤3 tareas activas, Yellow=cargado 4-6, Red=saturado ≥7). Usa este endpoint cuando el usuario pregunte qué equipo tiene más disponibilidad o capacidad para un nuevo proyecto.");

        // ── HU-IA-03: Buscar tarea semántica ─────────────────────────────────
        group.MapGet("/tasks/search", async (string q, HttpContext http, IAppDbContext db, ISender sender) =>
        {
            if (string.IsNullOrWhiteSpace(q))
                return Results.BadRequest("El parámetro 'q' es obligatorio.");
            var person = await ResolvePersonAsync(http, db);
            var result = await sender.Send(new AgentSearchTasksQuery(q, person?.Id ?? 0));
            return Results.Ok(result);
        })
        .WithName("search_tasks")
        .WithSummary("Buscar tarea por descripción (búsqueda semántica)")
        .WithDescription("Busca tareas cuya descripción coincida con el texto proporcionado usando similitud semántica. Usa este endpoint para identificar una tarea concreta cuando el usuario la mencione de forma natural, por ejemplo 'la tarea del proxy' o 'lo del certificado SSL'. Devuelve las 5 tareas más similares con su puntuación.");

        // ── HU-IA-03: Cambiar estado de tarea ────────────────────────────────
        group.MapPost("/tasks/{id:int}/status", async (int id, AgentStatusRequest req, HttpContext http, IAppDbContext db, ISender sender) =>
        {
            var person = await ResolvePersonAsync(http, db);
            if (person is null) return Results.Problem("Usuario no encontrado.", statusCode: 404);
            await sender.Send(new AgentUpdateTaskStatusCommand(person.Id, id, req.Status));
            return Results.Ok(new { message = $"Estado de la tarea {id} actualizado a '{req.Status}'." });
        })
        .WithName("update_task_status")
        .WithSummary("Cambiar el estado de una tarea")
        .WithDescription("Actualiza el estado de una tarea. Estados válidos: Backlog, ToDo, InProgress, Blocked, Done. Una tarea Done es terminal y no puede cambiar. Úsalo cuando el usuario diga que ha terminado una tarea, que está bloqueado, o que empieza a trabajar en algo. IMPORTANTE: confirma siempre con el usuario antes de ejecutar.");

        // ── HU-IA-04: Crear tarea ─────────────────────────────────────────────
        group.MapPost("/tasks", async (AgentCreateTaskRequest req, HttpContext http, IAppDbContext db, ISender sender) =>
        {
            var person = await ResolvePersonAsync(http, db);
            if (person is null) return Results.Problem("Usuario no encontrado.", statusCode: 404);
            var id = await sender.Send(new AgentCreateTaskCommand(
                person.Id, req.ProjectId, req.Title, req.Description,
                req.Priority ?? "Medium", req.EpicId, req.SprintId, req.AssignToSelf ?? true));
            return Results.Created($"/api/projects/{req.ProjectId}/workitems/{id}", new { id, message = $"Tarea '{req.Title}' creada con ID {id}." });
        })
        .WithName("create_task")
        .WithSummary("Crear una nueva tarea")
        .WithDescription("Crea una nueva tarea en un proyecto. Requiere el ID del proyecto. La prioridad puede ser: Low, Medium, High, Critical. Si assignToSelf es true, la tarea se asigna automáticamente al usuario. Úsalo cuando el usuario quiera registrar trabajo pendiente.");

        // ── HU-IA-06: Añadir comentario ───────────────────────────────────────
        group.MapPost("/tasks/{id:int}/comment", async (int id, AgentCommentRequest req, HttpContext http, IAppDbContext db, ISender sender) =>
        {
            var person = await ResolvePersonAsync(http, db);
            if (person is null) return Results.Problem("Usuario no encontrado.", statusCode: 404);
            await sender.Send(new AgentAddCommentCommand(person.Id, id, req.Text));
            return Results.Ok(new { message = $"Comentario añadido a la tarea {id}." });
        })
        .WithName("add_task_comment")
        .WithSummary("Añadir un comentario o nota de seguimiento a una tarea")
        .WithDescription("Añade un comentario de seguimiento a una tarea existente. El comentario queda registrado con el autor y la fecha. Úsalo cuando el usuario quiera documentar avances, bloqueos o novedades sobre una tarea.");

        // ── Admin: Reindexar embeddings ───────────────────────────────────────
        group.MapPost("/reindex", async (ISender sender) =>
        {
            var result = await sender.Send(new AgentReindexCommand());
            return Results.Ok(result);
        })
        .WithName("reindex_embeddings")
        .WithSummary("Regenerar índice de embeddings para búsqueda semántica")
        .WithDescription("Genera o actualiza los embeddings vectoriales de todas las tareas para permitir búsqueda semántica. Ejecutar después de crear o modificar tareas masivamente. Puede tardar varios segundos según el número de tareas.");

        // ── Charts: almacenamiento temporal de imágenes ───────────────────────
        group.MapPost("/charts", async (HttpRequest request, IConfiguration config, CancellationToken ct) =>
        {
            using var ms = new System.IO.MemoryStream();
            await request.Body.CopyToAsync(ms, ct);
            var id = ChartStore.Store(ms.ToArray());
            var externalUrl = config["Agent:ExternalUrl"] ?? "http://localhost:5000";
            return Results.Ok(new { id, url = $"{externalUrl}/api/agent/charts/{id}" });
        })
        .WithName("store_chart")
        .ExcludeFromDescription();  // no exponer en el spec del agente
    }

    // Endpoint público (sin API key) para servir las imágenes al navegador
    public static void MapAgentChartEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/agent/charts/{id}", (string id) =>
        {
            var data = ChartStore.Get(id);
            return data is null
                ? Results.NotFound()
                : Results.File(data, "image/png");
        })
        .ExcludeFromDescription();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static async ValueTask<object?> AgentApiKeyFilter(EndpointFilterInvocationContext ctx, EndpointFilterDelegate next)
    {
        var config = ctx.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var configuredKey = config["Agent:ApiKey"];

        if (!string.IsNullOrEmpty(configuredKey))
        {
            var auth = ctx.HttpContext.Request.Headers.Authorization.ToString();
            var key  = auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? auth[7..] : auth;
            if (key != configuredKey)
                return Results.Json(new { error = "API key inválida." }, statusCode: 401);
        }

        return await next(ctx);
    }

    private static async Task<CarteraProyectos.Core.Domain.Person?> ResolvePersonAsync(HttpContext http, IAppDbContext db)
    {
        var email = http.Request.Headers["X-Open-WebUI-User-Email"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(email)) return null;
        return await db.Persons.FirstOrDefaultAsync(p => p.Email == email);
    }
}

// ── Chart store (in-memory, TTL no necesario — los charts son efímeros) ─────────

internal static class ChartStore
{
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, byte[]> _store = new();

    public static string Store(byte[] data)
    {
        var id = Guid.NewGuid().ToString("N");
        _store[id] = data;
        return id;
    }

    public static byte[]? Get(string id) => _store.TryGetValue(id, out var data) ? data : null;
}

// ── Request bodies ────────────────────────────────────────────────────────────

public record AgentStatusRequest(string Status);

public record AgentCreateTaskRequest(
    int ProjectId, string Title, string? Description,
    string? Priority, int? EpicId, int? SprintId, bool? AssignToSelf);

public record AgentCommentRequest(string Text);
