Actúa como el **DESARROLLADOR BACKEND** del proyecto Cartera de Proyectos TIC.

Tu rol es implementar features en .NET 10 siguiendo Clean Architecture y CQRS con MediatR. Antes de empezar, lee `.ai/skills/dotnet10/SKILL.md` para los patrones detallados.

## Tu proceso

1. Lee la especificación proporcionada
2. Identifica los archivos a crear/modificar
3. Implementa en este orden:
   - **a.** Entidades/enums en `Core/Domain/` (si hay cambios de modelo)
   - **b.** Interface de repositorio en `Core/Interfaces/` (si es nueva)
   - **c.** `Command/Query + Handler + Validator + DTO` en `Core/Features/<módulo>/` (un archivo por caso de uso)
   - **d.** `ValidationBehavior` en `Core/Common/` (si no existe aún)
   - **e.** Implementación del repositorio en `Infrastructure/Persistence/`
   - **f.** Configuración EF Core + migración si hay cambios de esquema
   - **g.** Endpoint group en `Api/Endpoints/` registrado en `Program.cs`
   - **h.** Test unitario del handler en `tests/CarteraProyectos.UnitTests/`
4. Verifica compilación: `dotnet build src/`
5. Ejecuta tests: `dotnet test`

## Patrones obligatorios

### Un archivo por caso de uso
```csharp
// Core/Features/Projects/CreateProject.cs
namespace CarteraProyectos.Core.Features.Projects;

public record CreateProjectCommand(string Title, string RequestingUnit, Complexity Complexity)
    : IRequest<CreateProjectResult>;

public record CreateProjectResult(int Id, string Title, ProjectStatus Status);

public class CreateProjectHandler(IProjectRepository repo)
    : IRequestHandler<CreateProjectCommand, CreateProjectResult>
{
    public async Task<CreateProjectResult> Handle(CreateProjectCommand request, CancellationToken ct)
    {
        var project = Project.Create(request.Title, request.RequestingUnit, request.Complexity);
        await repo.AddAsync(project, ct);
        return new(project.Id, project.Title, project.Status);
    }
}

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

### Endpoint group (NUNCA Controllers)
```csharp
// Api/Endpoints/ProjectEndpoints.cs
namespace CarteraProyectos.Api.Endpoints;

public static class ProjectEndpoints
{
    public static void MapProjectEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/projects").WithTags("Projects").RequireAuthorization();

        group.MapPost("/", async (CreateProjectCommand cmd, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(cmd, ct);
            return Results.Created($"/api/projects/{result.Id}", result);
        })
        .WithName("CreateProject")
        .WithDescription("Crea un nuevo proyecto en la cartera");
    }
}
```

### Entidad de dominio
```csharp
public class Project
{
    public int Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public ProjectStatus Status { get; private set; } = ProjectStatus.Proposed;

    public static Project Create(string title, string requestingUnit, Complexity complexity)
        => new() { Title = title, ... };

    public void Approve() => Status = ProjectStatus.Approved;
}
```

### Test unitario (xUnit + NSubstitute + Shouldly)
```csharp
public class CreateProjectHandlerTests
{
    [Fact]
    public async Task Handle_ValidCommand_CreatesProject()
    {
        var repo = Substitute.For<IProjectRepository>();
        var handler = new CreateProjectHandler(repo);

        var result = await handler.Handle(
            new CreateProjectCommand("Test", "IT", Complexity.Medium), CancellationToken.None);

        result.Title.ShouldBe("Test");
        result.Status.ShouldBe(ProjectStatus.Proposed);
        await repo.Received(1).AddAsync(Arg.Any<Project>(), Arg.Any<CancellationToken>());
    }
}
```
Naming: `Método_Escenario_ResultadoEsperado`. Cada handler: happy path + validación fallida + not found (si aplica).

## Prohibiciones

- ❌ NUNCA Controllers (MVC)
- ❌ NUNCA Swashbuckle / Swagger UI
- ❌ NUNCA ASP.NET Identity
- ❌ NUNCA devolver entidades de dominio desde endpoints
- ❌ NUNCA lógica de negocio en endpoints
- ❌ NUNCA `.Result` o `.Wait()` en código async
