using System.Text.Json.Serialization;
using CarteraProyectos.Core.Common;
using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Features.Persons;
using CarteraProyectos.Core.Features.Projects;
using CarteraProyectos.Core.Features.Teams;
using CarteraProyectos.Infrastructure.Persistence;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// JSON: enums como strings, DateOnly compatible
builder.Services.ConfigureHttpJsonOptions(opts =>
{
    opts.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(opts =>
{
    opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Auth
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Auth:Authority"];
        options.RequireHttpsMetadata = false; // dev only
        options.TokenValidationParameters.ValidIssuer = builder.Configuration["Auth:ValidIssuer"];
        options.TokenValidationParameters.ValidateAudience = false;
        options.MapInboundClaims = false;
    });
builder.Services.AddAuthorization();

// MediatR + FluentValidation
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(ValidationBehavior<,>).Assembly));
builder.Services.AddValidatorsFromAssembly(typeof(ValidationBehavior<,>).Assembly);
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
builder.Services.AddScoped<CarteraProyectos.Core.Interfaces.IAppDbContext>(
    sp => sp.GetRequiredService<AppDbContext>());

// OpenAPI
builder.Services.AddOpenApi(options =>
{
    // Declara el security scheme Bearer JWT en components/securitySchemes
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Token JWT obtenido de Keycloak. Introduce el token sin el prefijo 'Bearer '."
        };
        return Task.CompletedTask;
    });

    // Marca con el requisito Bearer todos los endpoints que tengan metadatos de autorización
    options.AddOperationTransformer((operation, context, cancellationToken) =>
    {
        var hasAuthorize = context.Description.ActionDescriptor.EndpointMetadata
            .OfType<IAuthorizeData>()
            .Any();

        if (hasAuthorize)
        {
            operation.Security ??= [];
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", context.Document)] = []
            });
        }

        return Task.CompletedTask;
    });
});

// CORS
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:4200"])
              .AllowAnyHeader()
              .AllowAnyMethod()));

// Health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>();

var app = builder.Build();

// Migrate DB on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

app.UseExceptionHandler(errorApp => errorApp.Run(async ctx =>
{
    var ex = ctx.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
    if (ex is null) return;

    var (status, title) = ex switch
    {
        FluentValidation.ValidationException ve => (400, ve.Message),
        KeyNotFoundException                    => (404, ex.Message),
        UnauthorizedAccessException             => (403, ex.Message),
        InvalidOperationException               => (422, ex.Message),
        _                                       => (500, "Error interno del servidor.")
    };

    ctx.Response.StatusCode = status;
    ctx.Response.ContentType = "application/problem+json";
    await ctx.Response.WriteAsJsonAsync(new { status, title });
}));

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// OpenAPI + Scalar
app.MapOpenApi();
app.MapScalarApiReference();

// Health check
app.MapHealthChecks("/health");

// ──────────────────────────────────────────────────────────────
// /api/me
// ──────────────────────────────────────────────────────────────
app.MapGet("/api/me", async (HttpContext ctx, AppDbContext db) =>
{
    var sub = ctx.User.FindFirst("sub")?.Value;
    if (sub is null) return Results.Unauthorized();

    var person = await db.Persons.FirstOrDefaultAsync(p => p.SubjectId == sub);
    if (person is null)
    {
        var name = ctx.User.FindFirst("name")?.Value
                ?? ctx.User.FindFirst("preferred_username")?.Value
                ?? "Unknown";
        var email = ctx.User.FindFirst("email")?.Value ?? "";

        var initialGestorEmails = app.Configuration
            .GetSection("Admin:InitialGestorEmails")
            .Get<string[]>() ?? [];

        var role = initialGestorEmails.Contains(email, StringComparer.OrdinalIgnoreCase)
            ? PersonRole.Gestor
            : PersonRole.Desarrollador;

        person = Person.CreateFromClaims(sub, name, email, role);
        db.Persons.Add(person);
        await db.SaveChangesAsync();
    }

    return Results.Ok(new
    {
        person.Id,
        person.SubjectId,
        person.Name,
        person.Email,
        Role = person.Role.ToString()
    });
})
.RequireAuthorization()
.WithName("GetCurrentUser")
.WithDescription("Devuelve la información del usuario autenticado. Crea el usuario si no existe.");

// ──────────────────────────────────────────────────────────────
// /api/persons
// ──────────────────────────────────────────────────────────────
var persons = app.MapGroup("/api/persons").RequireAuthorization();

persons.MapGet("/", async (IMediator mediator, CancellationToken ct, int page = 1, int pageSize = 20) =>
    Results.Ok(await mediator.Send(new GetPersonsQuery(page, pageSize), ct)))
    .WithName("GetPersons")
    .WithDescription("Lista todas las personas registradas en el sistema. Soporta paginación con page y pageSize (máx 100).");

persons.MapPut("/{id:int}/role", async (int id, UpdatePersonRoleRequest req, IMediator mediator, CancellationToken ct) =>
{
    if (!Enum.TryParse<PersonRole>(req.Role, out var role))
        return Results.BadRequest("Rol no válido. Valores aceptados: Desarrollador, JefeEquipo, Gestor.");
    await mediator.Send(new UpdatePersonRoleCommand(id, role), ct);
    return Results.NoContent();
})
.WithName("UpdatePersonRole")
.WithDescription("Actualiza el rol de una persona. Solo accesible por el Gestor.");

// ──────────────────────────────────────────────────────────────
// /api/teams
// ──────────────────────────────────────────────────────────────
var teams = app.MapGroup("/api/teams").RequireAuthorization();

teams.MapGet("/", async (IMediator mediator, CancellationToken ct, int page = 1, int pageSize = 20) =>
    Results.Ok(await mediator.Send(new GetTeamsQuery(page, pageSize), ct)))
    .WithName("GetTeams")
    .WithDescription("Lista todos los equipos. Soporta paginación con page y pageSize (máx 100).");

teams.MapGet("/{id:int}", async (int id, IMediator mediator, CancellationToken ct) =>
{
    var result = await mediator.Send(new GetTeamQuery(id), ct);
    return result is null ? Results.NotFound() : Results.Ok(result);
})
.WithName("GetTeam")
.WithDescription("Devuelve el detalle de un equipo con sus miembros.");

teams.MapPost("/", async (CreateTeamCommand cmd, IMediator mediator, CancellationToken ct) =>
{
    var id = await mediator.Send(cmd, ct);
    return Results.Created($"/api/teams/{id}", new { id });
})
.WithName("CreateTeam")
.WithDescription("Crea un nuevo equipo. Solo Gestor.");

teams.MapPut("/{id:int}", async (int id, CreateTeamCommand body, IMediator mediator, CancellationToken ct) =>
{
    await mediator.Send(new UpdateTeamCommand(id, body.Name, body.Description, body.LeadPersonId), ct);
    return Results.NoContent();
})
.WithName("UpdateTeam")
.WithDescription("Actualiza un equipo existente. Solo Gestor.");

teams.MapDelete("/{id:int}", async (int id, IMediator mediator, CancellationToken ct) =>
{
    await mediator.Send(new DeleteTeamCommand(id), ct);
    return Results.NoContent();
})
.WithName("DeleteTeam")
.WithDescription("Elimina un equipo. Falla si tiene proyectos activos. Solo Gestor.");

teams.MapPost("/{id:int}/members", async (int id, AssignMemberRequest req, IMediator mediator, CancellationToken ct) =>
{
    await mediator.Send(new AssignPersonToTeamCommand(id, req.PersonId), ct);
    return Results.NoContent();
})
.WithName("AssignPersonToTeam")
.WithDescription("Asigna una persona a un equipo. Solo Gestor.");

teams.MapDelete("/{id:int}/members/{personId:int}", async (int id, int personId, IMediator mediator, CancellationToken ct) =>
{
    await mediator.Send(new RemovePersonFromTeamCommand(id, personId), ct);
    return Results.NoContent();
})
.WithName("RemovePersonFromTeam")
.WithDescription("Elimina a una persona de un equipo. Solo Gestor.");

// ──────────────────────────────────────────────────────────────
// /api/projects
// ──────────────────────────────────────────────────────────────
var projects = app.MapGroup("/api/projects").RequireAuthorization();

projects.MapGet("/", async (
    IMediator mediator, CancellationToken ct,
    string? status, int? year, int? teamId, string? complexity, string? q,
    int page = 1, int pageSize = 20) =>
{
    ProjectStatus? st = status is not null && Enum.TryParse<ProjectStatus>(status, out var s) ? s : null;
    ProjectComplexity? cx = complexity is not null && Enum.TryParse<ProjectComplexity>(complexity, out var c) ? c : null;
    return Results.Ok(await mediator.Send(new GetProjectsQuery(st, year, teamId, cx, q, page, pageSize), ct));
})
.WithName("GetProjects")
.WithDescription("Lista proyectos con filtros opcionales: status, year, teamId, complexity, q (búsqueda de texto). Soporta paginación con page y pageSize (máx 100).");

projects.MapGet("/{id:int}", async (int id, IMediator mediator, CancellationToken ct) =>
{
    var result = await mediator.Send(new GetProjectQuery(id), ct);
    return result is null ? Results.NotFound() : Results.Ok(result);
})
.WithName("GetProject")
.WithDescription("Devuelve el detalle de un proyecto con sus equipos asignados.");

projects.MapPost("/", async (CreateProjectCommand cmd, IMediator mediator, CancellationToken ct) =>
{
    var id = await mediator.Send(cmd, ct);
    return Results.Created($"/api/projects/{id}", new { id });
})
.WithName("CreateProject")
.WithDescription("Crea un nuevo proyecto en estado Propuesto. Solo Gestor.");

projects.MapPut("/{id:int}", async (int id, CreateProjectCommand body, IMediator mediator, CancellationToken ct) =>
{
    await mediator.Send(new UpdateProjectCommand(id, body.Title, body.Description, body.RequestingUnit,
        body.Complexity, body.PortfolioYear, body.StartDate, body.EndDate), ct);
    return Results.NoContent();
})
.WithName("UpdateProject")
.WithDescription("Actualiza los datos de un proyecto. Solo Gestor.");

projects.MapDelete("/{id:int}", async (int id, IMediator mediator, CancellationToken ct) =>
{
    await mediator.Send(new DeleteProjectCommand(id), ct);
    return Results.NoContent();
})
.WithName("DeleteProject")
.WithDescription("Elimina un proyecto. Solo en estado Propuesto o Cancelado. Solo Gestor.");

projects.MapPost("/{id:int}/status", async (int id, TransitionStatusRequest req, HttpContext ctx, AppDbContext db, IMediator mediator, CancellationToken ct) =>
{
    if (!Enum.TryParse<ProjectStatus>(req.Status, out var newStatus))
        return Results.BadRequest("Estado no válido.");

    var sub = ctx.User.FindFirst("sub")?.Value;
    var person = sub is not null ? await db.Persons.FirstOrDefaultAsync(p => p.SubjectId == sub, ct) : null;
    if (person is null) return Results.Unauthorized();

    await mediator.Send(new TransitionProjectStatusCommand(id, newStatus, person.Id), ct);
    return Results.NoContent();
})
.WithName("TransitionProjectStatus")
.WithDescription("Cambia el estado de un proyecto según la máquina de estados. Gestor o JefeEquipo del proyecto.");

projects.MapPost("/{id:int}/teams", async (int id, AssignTeamRequest req, IMediator mediator, CancellationToken ct) =>
{
    await mediator.Send(new AssignTeamToProjectCommand(id, req.TeamId, req.IsPrimary), ct);
    return Results.NoContent();
})
.WithName("AssignTeamToProject")
.WithDescription("Asigna un equipo a un proyecto. Solo Gestor.");

projects.MapDelete("/{id:int}/teams/{teamId:int}", async (int id, int teamId, IMediator mediator, CancellationToken ct) =>
{
    await mediator.Send(new RemoveTeamFromProjectCommand(id, teamId), ct);
    return Results.NoContent();
})
.WithName("RemoveTeamFromProject")
.WithDescription("Desasigna un equipo de un proyecto. Solo Gestor.");

app.Run();

// ──────────────────────────────────────────────────────────────
// Request DTOs (solo para binding de endpoints, no son de dominio)
// ──────────────────────────────────────────────────────────────
record UpdatePersonRoleRequest(string Role);
record AssignMemberRequest(int PersonId);
record TransitionStatusRequest(string Status);
record AssignTeamRequest(int TeamId, bool IsPrimary);
