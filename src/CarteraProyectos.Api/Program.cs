using System.Text.Json.Serialization;
using CarteraProyectos.Api.Endpoints;
using CarteraProyectos.Core.Common;
using CarteraProyectos.Core.Interfaces;
using CarteraProyectos.Infrastructure.Persistence;
using CarteraProyectos.Infrastructure.Services;
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
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AgentAuditBehavior<,>));
builder.Services.AddScoped<CarteraProyectos.Core.Interfaces.IAppDbContext>(
    sp => sp.GetRequiredService<AppDbContext>());

// MemoryCache (necesario para IEphemeralBlobStore y otros servicios singleton)
builder.Services.AddMemoryCache();

// Almacén efímero de blobs para exports/gráficos generados por el agente IA
builder.Services.AddSingleton<CarteraProyectos.Core.Interfaces.IEphemeralBlobStore,
    CarteraProyectos.Infrastructure.Services.MemoryCacheBlobStore>();
builder.Services.AddSingleton<CarteraProyectos.Core.Interfaces.IPublicUrlProvider,
    CarteraProyectos.Infrastructure.Services.ConfigPublicUrlProvider>();

// Embedding service (Bedrock)
builder.Services.AddSingleton<IEmbeddingService, BedrockEmbeddingService>();

// Chat completion client (LiteLLM)
builder.Services.AddSingleton<IChatCompletionClient, LiteLlmChatCompletionClient>();

// Identity Provider service (Keycloak Admin API)
builder.Services.AddHttpClient<CarteraProyectos.Infrastructure.Identity.KeycloakAdminService>();
builder.Services.AddScoped<CarteraProyectos.Core.Interfaces.IIdentityProviderService,
    CarteraProyectos.Infrastructure.Identity.KeycloakAdminService>();

// OpenAPI (frontend)
builder.Services.AddOpenApi(options =>
{
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

// Migrate DB on startup + seed demo data if empty
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    if (app.Environment.IsDevelopment())
        await CarteraProyectos.Infrastructure.Persistence.DataSeeder.SeedAsync(db);
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
app.MapOpenApi();  // sirve /openapi/v1.json y /openapi/agent.json automáticamente
app.MapScalarApiReference();

// Health check
app.MapHealthChecks("/health");

// Endpoints
app.MapUserEndpoints();
app.MapPersonEndpoints();
app.MapTeamEndpoints();
app.MapProjectEndpoints();
app.MapEpicEndpoints();
app.MapSprintEndpoints();
app.MapWorkItemEndpoints();
app.MapCommentEndpoints();
app.MapDashboardEndpoints();
app.MapReportEndpoints();
app.MapChatEndpoints();
app.MapChatBlobEndpoints();
app.MapPromoterEndpoints();
app.MapOrganicUnitEndpoints();
app.MapTagEndpoints();
app.MapProjectRiskEndpoints();
app.MapProjectDependencyEndpoints();

app.Run();
