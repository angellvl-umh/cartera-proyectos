---
name: dotnet10-cleanarch-skill
description: .NET 10 backend patterns - Clean Architecture, Minimal APIs, MediatR CQRS, FluentValidation, EF Core, pgvector. Use when generating or reviewing backend C# code.
---

# .NET 10 Backend Skill

## Architecture

```
src/
├── CarteraProyectos.Api/            # Minimal APIs, Middleware, OpenAPI/Scalar
├── CarteraProyectos.Core/           # Entities, Handlers, Validators, Interfaces
└── CarteraProyectos.Infrastructure/ # EF Core, Repositories, pgvector, Auth
```

## Core: Vertical Slices por Feature

```
CarteraProyectos.Core/
├── Domain/              # Entities, Value Objects, Enums
├── Interfaces/          # Repository contracts
├── Features/
│   └── Projects/
│       ├── CreateProject.cs    # Command + Handler + Validator + DTO (one file)
│       ├── GetProject.cs       # Query + Handler + DTO
│       └── ListProjects.cs     # Query + Handler + DTO
└── Common/              # MediatR Behaviours, base exceptions
```

## Patterns

### Entity (Domain)
```csharp
namespace CarteraProyectos.Core.Domain;

public class Project
{
    public int Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string RequestingUnit { get; private set; } = string.Empty;
    public Complexity Complexity { get; private set; }
    public ProjectStatus Status { get; private set; } = ProjectStatus.Proposed;
    public int? PortfolioYear { get; private set; }
    public DateOnly? StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }

    public static Project Create(string title, string requestingUnit, Complexity complexity)
    {
        return new Project { Title = title, RequestingUnit = requestingUnit, Complexity = complexity };
    }

    public void Approve() => Status = ProjectStatus.Approved;
    public void Start() => Status = ProjectStatus.InProgress;
}
```

### Command + Handler + Validator (single file)
```csharp
namespace CarteraProyectos.Core.Features.Projects;

// Command
public record CreateProjectCommand(
    string Title,
    string RequestingUnit,
    Complexity Complexity,
    string? Description = null
) : IRequest<CreateProjectResult>;

// Result DTO
public record CreateProjectResult(int Id, string Title, ProjectStatus Status);

// Handler
public class CreateProjectHandler(IProjectRepository repo) : IRequestHandler<CreateProjectCommand, CreateProjectResult>
{
    public async Task<CreateProjectResult> Handle(CreateProjectCommand request, CancellationToken ct)
    {
        var project = Project.Create(request.Title, request.RequestingUnit, request.Complexity);
        await repo.AddAsync(project, ct);
        return new(project.Id, project.Title, project.Status);
    }
}

// Validator
public class CreateProjectValidator : AbstractValidator<CreateProjectCommand>
{
    public CreateProjectValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.RequestingUnit).NotEmpty();
        RuleFor(x => x.Complexity).IsInEnum();
    }
}
```

### Query + Handler
```csharp
namespace CarteraProyectos.Core.Features.Projects;

public record GetProjectQuery(int Id) : IRequest<ProjectDto?>;

public record ProjectDto(
    int Id, string Title, string? Description,
    string RequestingUnit, Complexity Complexity,
    ProjectStatus Status, int? PortfolioYear);

public class GetProjectHandler(IProjectRepository repo) : IRequestHandler<GetProjectQuery, ProjectDto?>
{
    public async Task<ProjectDto?> Handle(GetProjectQuery request, CancellationToken ct)
    {
        var project = await repo.GetByIdAsync(request.Id, ct);
        return project is null ? null : new(
            project.Id, project.Title, project.Description,
            project.RequestingUnit, project.Complexity,
            project.Status, project.PortfolioYear);
    }
}
```

### Minimal API Endpoint Group
```csharp
namespace CarteraProyectos.Api.Endpoints;

public static class ProjectEndpoints
{
    public static void MapProjectEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/projects").WithTags("Projects");

        group.MapPost("/", async (CreateProjectCommand cmd, ISender sender) =>
        {
            var result = await sender.Send(cmd);
            return Results.Created($"/api/projects/{result.Id}", result);
        })
        .WithName("CreateProject")
        .WithDescription("Create a new project in the portfolio");

        group.MapGet("/{id:int}", async (int id, ISender sender) =>
        {
            var result = await sender.Send(new GetProjectQuery(id));
            return result is not null ? Results.Ok(result) : Results.NotFound();
        })
        .WithName("GetProject");
    }
}
```

### Program.cs Registration
```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CreateProjectCommand).Assembly));
builder.Services.AddValidatorsFromAssembly(typeof(CreateProjectValidator).Assembly);
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
builder.Services.AddOpenApi();

var app = builder.Build();

app.MapOpenApi();
app.MapScalarApiReference();
app.MapProjectEndpoints();
app.MapTeamEndpoints();

app.Run();
```

### Validation Behaviour (MediatR Pipeline)
```csharp
namespace CarteraProyectos.Core.Common;

public class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (!validators.Any()) return await next();

        var context = new ValidationContext<TRequest>(request);
        var failures = (await Task.WhenAll(validators.Select(v => v.ValidateAsync(context, ct))))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count != 0)
            throw new ValidationException(failures);

        return await next();
    }
}
```

### Repository Interface
```csharp
namespace CarteraProyectos.Core.Interfaces;

public interface IProjectRepository
{
    Task<Project?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<List<Project>> ListAsync(ProjectFilter filter, CancellationToken ct = default);
    Task AddAsync(Project project, CancellationToken ct = default);
    Task UpdateAsync(Project project, CancellationToken ct = default);
}
```

### EF Core DbContext (Infrastructure)
```csharp
namespace CarteraProyectos.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<Person> Persons => Set<Person>();
    public DbSet<WorkItem> WorkItems => Set<WorkItem>();
    public DbSet<Epic> Epics => Set<Epic>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
```

## Conventions

- Primary constructors for handlers and services
- Records for Commands, Queries, DTOs
- One file per use case (Command + Handler + Validator + DTO)
- `async Task` always, never `.Result` or `.Wait()`
- Nullable reference types enabled
- `CancellationToken` propagated everywhere
- No Controllers, only Minimal API endpoint groups
- OpenAPI descriptions on every endpoint (for Tool Server)
- Enums stored as strings in DB

## Servicios de aplicación compartidos (handlers no llaman a otros handlers)

`ISender`/`IMediator` **solo** se inyecta en Minimal API endpoints y en `SendChatMessageHandler` (el orquestador del bucle de tool-calling del chat, que despacha dinámicamente los `Agent*Command` que decide el modelo). Ningún otro `IRequestHandler` debe depender de `ISender` para invocar a otro handler — verificado por `tests/CarteraProyectos.ArchTests` (`NoNestedMediatorHandlersTests`), que falla el build si se reintroduce el patrón.

Cuando un `Agent*Handler` necesita la misma lógica que ya tiene el handler de dominio equivalente (p. ej. `AgentUpdateTaskStatusHandler` y `TransitionWorkItemStatusHandler`), esa lógica se extrae a un servicio de aplicación plano en `Features/<Feature>/`, y ambos handlers lo inyectan directamente:

```csharp
// Features/WorkItems/WorkItemLifecycleService.cs
public interface IWorkItemLifecycleService
{
    Task TransitionStatusAsync(int id, WorkItemStatus newStatus, int requestingPersonId, CancellationToken ct);
}

public sealed class WorkItemLifecycleService(IAppDbContext db) : IWorkItemLifecycleService
{
    public async Task TransitionStatusAsync(int id, WorkItemStatus newStatus, int requestingPersonId, CancellationToken ct)
    {
        // ... lógica de negocio real, antes duplicada/anidada entre los dos handlers ...
    }
}

// Features/WorkItems/TransitionWorkItemStatus.cs — handler de dominio, adaptador fino
public sealed class TransitionWorkItemStatusHandler(IWorkItemLifecycleService service)
    : IRequestHandler<TransitionWorkItemStatusCommand>
{
    public Task Handle(TransitionWorkItemStatusCommand request, CancellationToken ct)
        => service.TransitionStatusAsync(request.Id, request.NewStatus, request.RequestingPersonId, ct);
}

// Features/Agent/AgentHandlers.cs — Agent handler, mismo servicio, sin ISender
public sealed class AgentUpdateTaskStatusHandler(IWorkItemLifecycleService service)
    : IRequestHandler<AgentUpdateTaskStatusCommand>
{
    public async Task Handle(AgentUpdateTaskStatusCommand request, CancellationToken ct)
    {
        if (!Enum.TryParse<WorkItemStatus>(request.NewStatus, out var status))
            throw new InvalidOperationException("Estado no válido.");
        await service.TransitionStatusAsync(request.WorkItemId, status, request.PersonId, ct);
    }
}
```

Cuando los comandos tienen muchos campos (p. ej. `CreateProjectCommand`), el servicio puede recibir el propio record como parámetro en vez de desglosarlo campo a campo — sigue siendo un simple DTO en ese punto, no se envía por `ISender`.

## Testing

### Unit Test (Handler)
```csharp
public class CreateProjectHandlerTests
{
    [Fact]
    public async Task Handle_ValidCommand_CreatesProject()
    {
        var repo = Substitute.For<IProjectRepository>();
        var handler = new CreateProjectHandler(repo);
        var cmd = new CreateProjectCommand("Test", "IT", Complexity.Medium);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.Title.ShouldBe("Test");
        result.Status.ShouldBe(ProjectStatus.Proposed);
        await repo.Received(1).AddAsync(Arg.Any<Project>(), Arg.Any<CancellationToken>());
    }
}
```

## Prohibiciones
- ❌ NUNCA usar Controllers (MVC)
- ❌ NUNCA usar Swashbuckle / Swagger UI
- ❌ NUNCA usar ASP.NET Identity
- ❌ NUNCA usar constructor injection clásica (usar primary constructors)
- ❌ NUNCA devolver entidades de dominio desde los endpoints
- ❌ NUNCA poner lógica de negocio en los endpoints
- ❌ NUNCA inyectar `ISender`/`IMediator` en un `IRequestHandler` (salvo `SendChatMessageHandler`) — extrae la lógica compartida a un servicio de aplicación
