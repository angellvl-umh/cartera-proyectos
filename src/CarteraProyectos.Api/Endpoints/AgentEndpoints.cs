using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Features.Agent;
using CarteraProyectos.Core.Features.Projects.WeeklyUpdates;
using CarteraProyectos.Core.Features.Reports;
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
            .AddEndpointFilter(AgentApiKeyFilter)
            .RequireRateLimiting("agent");

        // ── HU-IA-07: Mis tareas ──────────────────────────────────────────────
        group.MapGet("/me", async (HttpContext http, IAppDbContext db, ISender sender) =>
        {
            var person = await ResolvePersonAsync(http, db);
            var guardResult = Guard(person);
            if (guardResult is not null) return guardResult;
            var result = await sender.Send(new AgentGetMyTasksQuery(person!.Id));
            return Results.Ok(result);
        })
        .WithName("get_my_tasks")
        .WithSummary("Obtener mis tareas pendientes")
        .WithDescription("Devuelve las tareas activas, backlog y completadas del usuario que realiza la consulta. Usa este endpoint cuando el usuario pregunte '¿qué tengo pendiente?', '¿en qué estoy trabajando?', '¿cuáles son mis tareas?'.");

        // ── HU-IA-01: Lista de proyectos ──────────────────────────────────────
        group.MapGet("/projects", async (HttpContext http, IAppDbContext db, ISender sender,
            string? status) =>
        {
            var person = await ResolvePersonAsync(http, db);
            var guardResult = Guard(person);
            if (guardResult is not null) return guardResult;
            var result = await sender.Send(new AgentGetProjectsQuery(person!.Id, status));
            return Results.Ok(result);
        })
        .WithName("get_projects")
        .WithSummary("Listar proyectos del usuario")
        .WithDescription("Devuelve los proyectos asociados a los equipos del usuario con su estado, equipo principal (primaryTeamName) y progreso de tareas. Filtra opcionalmente por status (Stopped, PlanningWithClient, WaitingForDevelopers, PlanningSprint, InSprint, DevelopmentOutsideSprint, InTesting, Completed, PostponedByClient). Usa este endpoint cuando el usuario pregunte por sus proyectos o el estado general de la cartera.");

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
            var guardResult = Guard(person);
            if (guardResult is not null) return guardResult;
            await sender.Send(new AgentUpdateTaskStatusCommand(person!.Id, id, req.Status));
            return Results.Ok(new { message = $"Estado de la tarea {id} actualizado a '{req.Status}'." });
        })
        .WithName("update_task_status")
        .WithSummary("Cambiar el estado de una tarea")
        .WithDescription("Actualiza el estado de una tarea. Estados válidos: Backlog, ToDo, InProgress, Blocked, Done, Discarded. Done y Discarded son terminales y no pueden retroceder. Pueden hacerlo el Gestor y cualquier miembro de un equipo asignado al proyecto de la tarea (no solo los asignados). Úsalo cuando el usuario diga que ha terminado una tarea, que está bloqueado, que empieza a trabajar en algo o que quiere descartar una tarea. IMPORTANTE: confirma siempre con el usuario antes de ejecutar.");

        // ── HU-IA-04: Crear tarea ─────────────────────────────────────────────
        group.MapPost("/tasks", async (AgentCreateTaskRequest req, HttpContext http, IAppDbContext db, ISender sender) =>
        {
            var person = await ResolvePersonAsync(http, db);
            var guardResult = Guard(person);
            if (guardResult is not null) return guardResult;
            var id = await sender.Send(new AgentCreateTaskCommand(
                person!.Id, req.ProjectId, req.Title, req.Description,
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
            var guardResult = Guard(person);
            if (guardResult is not null) return guardResult;
            await sender.Send(new AgentAddCommentCommand(person!.Id, id, req.Text));
            return Results.Ok(new { message = $"Comentario añadido a la tarea {id}." });
        })
        .WithName("add_task_comment")
        .WithSummary("Añadir un comentario o nota de seguimiento a una tarea")
        .WithDescription("Añade un comentario de seguimiento a una tarea existente. El comentario queda registrado con el autor y la fecha. Úsalo cuando el usuario quiera documentar avances, bloqueos o novedades sobre una tarea.");

        // ── HU-IA-08: Añadir nota a proyecto ─────────────────────────────────
        group.MapPost("/projects/{id:int}/notes", async (int id, AgentNoteRequest req, HttpContext http, IAppDbContext db, ISender sender) =>
        {
            var person = await ResolvePersonAsync(http, db);
            var guardResult = Guard(person);
            if (guardResult is not null) return guardResult;
            var noteId = await sender.Send(new AgentAddProjectNoteCommand(person!.Id, id, req.Text));
            return Results.Created($"/api/projects/{id}/notes/{noteId}", new { id = noteId, message = $"Nota añadida al proyecto {id}." });
        })
        .WithName("add_project_note")
        .WithSummary("Añadir una nota de seguimiento a un proyecto")
        .WithDescription("Añade una nota o comentario de seguimiento a un proyecto existente. La nota queda registrada con el autor y la fecha. Úsalo cuando el usuario quiera documentar decisiones, hitos, bloqueos o novedades sobre un proyecto concreto.");

        // ── HU-IA-06b: Registrar avance semanal de proyecto ──────────────────
        group.MapPost("/projects/{id:int}/weekly-updates", async (int id, AgentWeeklyUpdateRequest req, HttpContext http, IAppDbContext db, ISender sender) =>
        {
            var person = await ResolvePersonAsync(http, db);
            var guardResult = Guard(person);
            if (guardResult is not null) return guardResult;
            if (!Enum.TryParse<ProjectHealthStatus>(req.HealthStatus, out var health))
                return Results.BadRequest("healthStatus debe ser OnTrack, AtRisk o Blocked.");
            var updateId = await sender.Send(new UpsertProjectWeeklyUpdateCommand(id, person!.Id, req.Summary, health));
            return Results.Ok(new { id = updateId, message = $"Avance semanal registrado para el proyecto {id}." });
        })
        .WithName("add_weekly_update")
        .WithSummary("Registrar el avance semanal de un proyecto")
        .WithDescription("Registra (o actualiza si ya existe uno esta semana) el avance semanal del usuario en un proyecto. healthStatus debe ser OnTrack (en curso), AtRisk (en riesgo) o Blocked (bloqueado). Solo se permite un registro por persona y proyecto por semana ISO; un segundo registro la misma semana actualiza el anterior. Úsalo cuando el usuario quiera reportar cómo va un proyecto esta semana.");

        // ── HU-IA-09: Informe semanal de cartera ───────────────────────────────
        group.MapGet("/weekly-portfolio-report", async (ISender sender, int? year = null, int? teamId = null) =>
        {
            var result = await sender.Send(new GetWeeklyPortfolioReportQuery(year, teamId));
            return Results.Ok(result);
        })
        .WithName("get_weekly_portfolio_report")
        .WithSummary("Informe semanal de seguimiento de cartera")
        .WithDescription("Devuelve proyectos en riesgo y otros clasificados por estado de actualización de esta semana. Filtrable por año y equipo. Usa este endpoint cuando necesites un resumen del estado de la cartera esta semana.");

        // ── Gestión de Personas ───────────────────────────────────────────────

        // ── 1. GET /persons ──────────────────────────────────────────────────
        group.MapGet("/persons", async (HttpContext http, IAppDbContext db, ISender sender, bool includeInactive = false) =>
        {
            var person = await ResolvePersonAsync(http, db);
            var guardResult = Guard(person);
            if (guardResult is not null) return guardResult;
            var result = await sender.Send(new AgentGetPersonsQuery(includeInactive));
            return Results.Ok(result);
        })
        .WithName("get_persons")
        .WithSummary("Listar personas registradas")
        .WithDescription("Lista las personas registradas con su rol (Desarrollador/Gestor), si están activas y si ya han iniciado sesión alguna vez. Úsalo para buscar a una persona por nombre o email antes de editarla o asignarle trabajo.");

        // ── 2. POST /persons (crear) ─────────────────────────────────────────
        group.MapPost("/persons", async (AgentPersonRequest req, HttpContext http, IAppDbContext db, ISender sender) =>
        {
            var person = await ResolvePersonAsync(http, db);
            var guardResult = Guard(person);
            if (guardResult is not null) return guardResult;
            var id = await sender.Send(new AgentCreatePersonCommand(person!.Id, req.Name, req.Email, req.Role));
            return Results.Created($"/api/persons/{id}", new { id, message = $"Persona '{req.Name}' pre-registrada con ID {id}." });
        })
        .WithName("create_person")
        .WithSummary("Pre-registrar una nueva persona")
        .WithDescription("Pre-registra una persona que se vinculará con su cuenta SSO en su primer inicio de sesión. role: Desarrollador o Gestor. Solo el Gestor puede hacerlo.");

        // ── 3. POST /persons/{id:int} (actualizar) ──────────────────────────
        group.MapPost("/persons/{id:int}", async (int id, AgentPersonRequest req, HttpContext http, IAppDbContext db, ISender sender) =>
        {
            var person = await ResolvePersonAsync(http, db);
            var guardResult = Guard(person);
            if (guardResult is not null) return guardResult;
            await sender.Send(new AgentUpdatePersonCommand(person!.Id, id, req.Name, req.Email, req.Role));
            return Results.Ok(new { message = $"Persona {id} actualizada." });
        })
        .WithName("update_person")
        .WithSummary("Actualizar datos de una persona")
        .WithDescription("Actualiza nombre, email y rol de una persona. Solo Gestor.");

        // ── 4. POST /persons/{id:int}/active (activar/desactivar) ───────────
        group.MapPost("/persons/{id:int}/active", async (int id, AgentPersonActiveRequest req, HttpContext http, IAppDbContext db, ISender sender) =>
        {
            var person = await ResolvePersonAsync(http, db);
            var guardResult = Guard(person);
            if (guardResult is not null) return guardResult;
            await sender.Send(new AgentSetPersonActiveCommand(person!.Id, id, req.IsActive));
            var action = req.IsActive ? "activada" : "desactivada";
            return Results.Ok(new { message = $"Persona {id} {action}." });
        })
        .WithName("set_person_active")
        .WithSummary("Activar o desactivar una persona")
        .WithDescription("Activa o desactiva una persona. Las personas inactivas no aparecen en listados ni pueden recibir tareas. Solo Gestor. Un Gestor no puede desactivarse a sí mismo.");

        // ── Gestión de Proyectos ──────────────────────────────────────────────

        // ── 5. POST /projects/{id:int}/status (cambiar estado) ─────────────
        group.MapPost("/projects/{id:int}/status", async (int id, AgentProjectStatusRequest req, HttpContext http, IAppDbContext db, ISender sender) =>
        {
            var person = await ResolvePersonAsync(http, db);
            var guardResult = Guard(person);
            if (guardResult is not null) return guardResult;
            await sender.Send(new AgentTransitionProjectStatusCommand(person!.Id, id, req.Status));
            return Results.Ok(new { message = $"Proyecto {id} transicionado a estado '{req.Status}'." });
        })
        .WithName("update_project_status")
        .WithSummary("Cambiar el estado de un proyecto")
        .WithDescription("Cambia el estado de un proyecto. Estados: Stopped, PlanningWithClient, WaitingForDevelopers, PlanningSprint, InSprint, DevelopmentOutsideSprint, InTesting, Completed, PostponedByClient. El grafo de transiciones se valida en el servidor (para Completed todos los sprints deben estar completados y las tareas Done o Discarded). Pueden hacerlo el Gestor y cualquier miembro de un equipo asignado al proyecto. IMPORTANTE: confirma siempre con el usuario antes de ejecutar.");

        // ── 5a. POST /projects (crear proyecto) ──────────────────────────────
        group.MapPost("/projects", async (AgentProjectCreateRequest req, HttpContext http, IAppDbContext db, ISender sender) =>
        {
            var person = await ResolvePersonAsync(http, db);
            var guardResult = Guard(person);
            if (guardResult is not null) return guardResult;
            var id = await sender.Send(new AgentCreateProjectCommand(
                person!.Id, req.Title, req.Description, req.RequestingUnit,
                req.Complexity, req.PortfolioYear, req.StartDate, req.EndDate,
                req.BeneficiaryCount, req.PromoterId, req.OrganicUnitId, req.GroupPriority,
                req.DesiredDeploymentDate, req.SpecificationsUrl,
                req.EpicUrl, req.EstimatedBudget, req.BusinessValue));
            return Results.Created($"/api/projects/{id}", new { id, message = $"Proyecto '{req.Title}' creado con ID {id}." });
        })
        .WithName("create_project")
        .WithSummary("Crear un nuevo proyecto en la cartera")
        .WithDescription("Crea un nuevo proyecto en la cartera. Solo el Gestor puede hacerlo. complexity: VerySmall, Small, Medium, Large, VeryLarge. Fechas en formato yyyy-MM-dd. businessValue (valor de negocio) y groupPriority entre 1 y 5. El proyecto se crea en estado Stopped; usa update_project_status para moverlo. IMPORTANTE: confirma siempre con el usuario antes de ejecutar.");

        // ── 5b. POST /projects/{id:int} (actualizar proyecto) ──────────────────
        group.MapPost("/projects/{id:int}", async (int id, AgentProjectUpdateRequest req, HttpContext http, IAppDbContext db, ISender sender) =>
        {
            var person = await ResolvePersonAsync(http, db);
            var guardResult = Guard(person);
            if (guardResult is not null) return guardResult;
            await sender.Send(new AgentUpdateProjectCommand(
                person!.Id, id, req.Title, req.Description, req.RequestingUnit,
                req.Complexity, req.PortfolioYear, req.StartDate, req.EndDate,
                req.BeneficiaryCount, req.PromoterId, req.OrganicUnitId, req.GroupPriority,
                req.DesiredDeploymentDate, req.SpecificationsUrl,
                req.EpicUrl, req.EstimatedBudget, req.BusinessValue));
            return Results.Ok(new { message = $"Proyecto {id} actualizado." });
        })
        .WithName("update_project")
        .WithSummary("Modificar los datos de un proyecto")
        .WithDescription("Actualización parcial de un proyecto: solo se modifican los campos enviados; los omitidos conservan su valor actual (no es posible vaciar un campo con esta tool). Solo el Gestor puede hacerlo. No cambia el estado del proyecto: para eso usa update_project_status. complexity: VerySmall, Small, Medium, Large, VeryLarge. Fechas en formato yyyy-MM-dd. IMPORTANTE: confirma siempre con el usuario antes de ejecutar.");

        // ── 6. GET /projects/{id:int}/risks (listar riesgos) ────────────────
        group.MapGet("/projects/{id:int}/risks", async (int id, ISender sender) =>
        {
            var result = await sender.Send(new AgentGetProjectRisksQuery(id));
            return Results.Ok(result);
        })
        .WithName("get_project_risks")
        .WithSummary("Listar riesgos del proyecto")
        .WithDescription("Riesgos del proyecto con probabilidad × impacto = severidad (1-9) y estado (Open/Mitigated/Closed).");

        // ── 7. POST /projects/{id:int}/risks (crear riesgo) ────────────────
        group.MapPost("/projects/{id:int}/risks", async (int id, AgentRiskRequest req, HttpContext http, IAppDbContext db, ISender sender) =>
        {
            var person = await ResolvePersonAsync(http, db);
            var guardResult = Guard(person);
            if (guardResult is not null) return guardResult;
            var riskId = await sender.Send(new AgentAddProjectRiskCommand(
                person!.Id, id, req.Description, req.Probability, req.Impact, req.MitigationPlan));
            return Results.Created($"/api/projects/{id}/risks/{riskId}", new { id = riskId, message = $"Riesgo creado en el proyecto {id}." });
        })
        .WithName("add_project_risk")
        .WithSummary("Registrar un riesgo del proyecto")
        .WithDescription("Registra un riesgo. probability e impact: Low, Medium o High. Gestor o miembro del equipo del proyecto.");

        // ── 8. POST /projects/{id:int}/risks/{riskId:int} (actualizar riesgo) ─
        group.MapPost("/projects/{id:int}/risks/{riskId:int}", async (int id, int riskId, AgentRiskUpdateRequest req, HttpContext http, IAppDbContext db, ISender sender) =>
        {
            var person = await ResolvePersonAsync(http, db);
            var guardResult = Guard(person);
            if (guardResult is not null) return guardResult;
            await sender.Send(new AgentUpdateProjectRiskCommand(
                person!.Id, id, riskId, req.Description, req.Probability, req.Impact, req.MitigationPlan, req.Status));
            return Results.Ok(new { message = $"Riesgo {riskId} del proyecto {id} actualizado." });
        })
        .WithName("update_project_risk")
        .WithSummary("Actualizar un riesgo del proyecto")
        .WithDescription("Actualiza un riesgo: descripción, niveles, plan de mitigación y estado (Open, Mitigated, Closed).");

        // ── 9. GET /projects/{id:int}/dependencies (listar dependencias) ──
        group.MapGet("/projects/{id:int}/dependencies", async (int id, ISender sender) =>
        {
            var result = await sender.Send(new AgentGetProjectDependenciesQuery(id));
            return Results.Ok(result);
        })
        .WithName("get_project_dependencies")
        .WithSummary("Listar dependencias del proyecto")
        .WithDescription("Dependencias del proyecto: de qué proyectos depende y qué proyectos dependen de él.");

        // ── 10. POST /projects/{id:int}/dependencies (crear dependencia) ──
        group.MapPost("/projects/{id:int}/dependencies", async (int id, AgentDependencyRequest req, HttpContext http, IAppDbContext db, ISender sender) =>
        {
            var person = await ResolvePersonAsync(http, db);
            var guardResult = Guard(person);
            if (guardResult is not null) return guardResult;
            var depId = await sender.Send(new AgentAddProjectDependencyCommand(
                person!.Id, id, req.DependsOnProjectId, req.Description));
            return Results.Created($"/api/projects/{id}/dependencies/{depId}", new { id = depId, message = $"Dependencia creada en el proyecto {id}." });
        })
        .WithName("add_project_dependency")
        .WithSummary("Registrar una dependencia entre proyectos")
        .WithDescription("Registra que este proyecto depende de otro. El servidor rechaza autodependencias, duplicados y ciclos directos.");

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
            var id = AgentBlobStore.Store(ms.ToArray(), "image/png", null);
            var externalUrl = config["Agent:ExternalUrl"] ?? "http://localhost:5000";
            return Results.Ok(new { id, url = $"{externalUrl}/api/agent/charts/{id}" });
        })
        .WithName("store_chart")
        .ExcludeFromDescription();  // no exponer en el spec del agente

        // ── Exports: almacenamiento temporal de ficheros (Excel, etc.) ─────────
        group.MapPost("/exports", async (HttpRequest request, IConfiguration config, string? fileName, CancellationToken ct) =>
        {
            using var ms = new System.IO.MemoryStream();
            await request.Body.CopyToAsync(ms, ct);
            const string xlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            var id = AgentBlobStore.Store(ms.ToArray(), xlsxContentType, fileName ?? "export.xlsx");
            var externalUrl = config["Agent:ExternalUrl"] ?? "http://localhost:5000";
            return Results.Ok(new { id, url = $"{externalUrl}/api/agent/exports/{id}" });
        })
        .WithName("store_export")
        .ExcludeFromDescription();  // no exponer en el spec del agente
    }

    // Endpoints públicos (sin API key) para servir ficheros al navegador
    public static void MapAgentChartEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/agent/charts/{id}", (string id) =>
        {
            var blob = AgentBlobStore.Get(id);
            return blob is null
                ? Results.NotFound()
                : Results.File(blob.Data, blob.ContentType);
        })
        .ExcludeFromDescription();

        app.MapGet("/api/agent/exports/{id}", (string id) =>
        {
            var blob = AgentBlobStore.Get(id);
            return blob is null
                ? Results.NotFound()
                : Results.File(blob.Data, blob.ContentType, blob.FileName);
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

    /// <summary>
    /// Valida que la persona exista y esté activa.
    /// Retorna null si todo está bien, o el resultado de error (404/403) si algo falta.
    /// </summary>
    private static IResult? Guard(Person? person)
    {
        if (person is null) 
            return Results.Problem("Usuario no encontrado.", statusCode: 404);
        if (!person.IsActive)
            return Results.Problem("Usuario inactivo.", statusCode: 403);
        return null;
    }
}

// ── Blob store (in-memory, TTL no necesario — los ficheros son efímeros) ────────

internal sealed record AgentBlob(byte[] Data, string ContentType, string? FileName);

internal static class AgentBlobStore
{
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, AgentBlob> _store = new();

    public static string Store(byte[] data, string contentType, string? fileName)
    {
        var id = Guid.NewGuid().ToString("N");
        _store[id] = new AgentBlob(data, contentType, fileName);
        return id;
    }

    public static AgentBlob? Get(string id) => _store.TryGetValue(id, out var blob) ? blob : null;
}

// ── Request bodies ────────────────────────────────────────────────────────────

public record AgentStatusRequest(string Status);

public record AgentCreateTaskRequest(
    int ProjectId, string Title, string? Description,
    string? Priority, int? EpicId, int? SprintId, bool? AssignToSelf);

public record AgentCommentRequest(string Text);

public record AgentNoteRequest(string Text);

public record AgentWeeklyUpdateRequest(string Summary, string HealthStatus);

public record AgentPersonRequest(string Name, string Email, string Role);

public record AgentPersonActiveRequest(bool IsActive);

public record AgentProjectStatusRequest(string Status);

public record AgentRiskRequest(string Description, string Probability, string Impact, string? MitigationPlan);

public record AgentRiskUpdateRequest(string Description, string Probability, string Impact, string? MitigationPlan, string Status);

public record AgentDependencyRequest(int DependsOnProjectId, string? Description);

public record AgentProjectCreateRequest(
    string Title, string? Description, string? RequestingUnit, string Complexity,
    int? PortfolioYear, DateOnly? StartDate, DateOnly? EndDate, int? BeneficiaryCount,
    int? PromoterId, int? OrganicUnitId, int? GroupPriority,
    DateOnly? DesiredDeploymentDate, string? SpecificationsUrl, string? EpicUrl,
    decimal? EstimatedBudget, int? BusinessValue);

public record AgentProjectUpdateRequest(
    string? Title, string? Description, string? RequestingUnit, string? Complexity,
    int? PortfolioYear, DateOnly? StartDate, DateOnly? EndDate, int? BeneficiaryCount,
    int? PromoterId, int? OrganicUnitId, int? GroupPriority,
    DateOnly? DesiredDeploymentDate, string? SpecificationsUrl, string? EpicUrl,
    decimal? EstimatedBudget, int? BusinessValue);
