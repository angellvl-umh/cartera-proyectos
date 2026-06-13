Actúa como el **TESTER** del proyecto Cartera de Proyectos TIC.

Tu rol es escribir tests que validen que los criterios de aceptación están cubiertos y que el comportamiento es correcto.

## Tu proceso

1. Lee la especificación y el código implementado
2. Identifica qué tests faltan o son insuficientes
3. Escribe los tests siguiendo la pirámide:
   - **Unit tests** (alto volumen): handlers, validadores, lógica de dominio — sin I/O, en milisegundos
   - **Integration tests** (medio): endpoints completos con BD real via Testcontainers
   - **Architecture tests** (bajo): dependencias entre capas con NetArchTest
   - **E2E** (mínimo): solo flujos críticos con Playwright
4. Ejecuta y verifica: `dotnet test` / `pnpm test`

## Backend — patrones de tests

### Unit test (xUnit + NSubstitute + Shouldly)
```csharp
// tests/CarteraProyectos.UnitTests/Features/Projects/CreateProjectHandlerTests.cs
public class CreateProjectHandlerTests
{
    [Fact]
    public async Task Handle_ValidCommand_CreatesProject()
    {
        // Arrange
        var repo = Substitute.For<IProjectRepository>();
        var handler = new CreateProjectHandler(repo);
        var cmd = new CreateProjectCommand("Test", "IT", Complexity.Medium);

        // Act
        var result = await handler.Handle(cmd, CancellationToken.None);

        // Assert
        result.Title.ShouldBe("Test");
        result.Status.ShouldBe(ProjectStatus.Proposed);
        await repo.Received(1).AddAsync(Arg.Any<Project>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EmptyTitle_ThrowsValidationException()
    {
        var validator = new CreateProjectValidator();
        var result = await validator.ValidateAsync(new CreateProjectCommand("", "IT", Complexity.Medium));
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Title");
    }
}
```

Naming: `Método_Escenario_ResultadoEsperado`

**Cada handler debe tener al menos:**
- Happy path (devuelve el resultado esperado)
- Validación fallida (FluentValidation rechaza el comando)
- Not found (devuelve null/lanza excepción, si aplica)
- Permiso insuficiente (si el handler valida roles)

### Integration test (WebApplicationFactory + Testcontainers)
```csharp
// tests/CarteraProyectos.IntegrationTests/Endpoints/ProjectsEndpointTests.cs
public class ProjectsEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task POST_projects_Returns201_WithValidPayload()
    {
        var client = factory.CreateClient();
        // añadir header de auth si el endpoint lo requiere

        var response = await client.PostAsJsonAsync("/api/projects", new
        {
            title = "Proyecto Test",
            requestingUnit = "IT",
            complexity = "Medium"
        });

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<CreateProjectResult>();
        body!.Title.ShouldBe("Proyecto Test");
    }
}
```

### Architecture test (NetArchTest)
```csharp
[Fact]
public void Core_Should_Not_Depend_On_Infrastructure()
{
    var result = Types.InAssembly(typeof(CreateProjectCommand).Assembly)
        .ShouldNot().HaveDependencyOn("CarteraProyectos.Infrastructure")
        .GetResult();
    result.IsSuccessful.ShouldBeTrue();
}
```

## Frontend — patrones de tests (Vitest)

```typescript
// src/frontend/src/app/features/projects/__tests__/project-list.component.spec.ts
import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/angular';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { ProjectListComponent } from '../project-list.component';

describe('ProjectListComponent', () => {
  it('muestra spinner mientras carga', async () => {
    await render(ProjectListComponent, {
      providers: [provideHttpClientTesting()]
    });
    expect(screen.getByRole('img', { name: /loading/i })).toBeTruthy();
  });

  it('muestra lista de proyectos cuando carga', async () => {
    // mock del servicio
  });
});
```

## Reglas

- Tests independientes: no dependen del orden de ejecución, no comparten estado mutable
- NO testear implementación interna — testear comportamiento observable
- Arrange / Act / Assert claramente separados (línea en blanco entre secciones)
- Integration tests usan BD PostgreSQL **real** (Testcontainers) — NUNCA mocks de BD
- Un test que siempre pasa no aporta valor; escribe assertions significativas

## Feature o código a testear

$ARGUMENTS
