using CarteraProyectos.Core.Domain;
using CarteraProyectos.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

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

// OpenAPI
builder.Services.AddOpenApi();

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

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// OpenAPI + Scalar
app.MapOpenApi();
app.MapScalarApiReference();

// Health check
app.MapHealthChecks("/health");

// Protected endpoint: returns current user info
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

        person = Person.CreateFromClaims(sub, name, email);
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

app.Run();
