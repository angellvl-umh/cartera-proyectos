using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Features.Projects.Dependencies;
using CarteraProyectos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace CarteraProyectos.UnitTests.Features.Projects.Dependencies;

public class ProjectDependencyHandlerTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static async Task<(Person gestor, Project projectA, Project projectB)> SeedTwoProjectsAsync(AppDbContext db)
    {
        var gestor = Person.CreateFromClaims("sub-g", "Gestor", "g@test.com", PersonRole.Gestor);
        var projectA = Project.Create("Proyecto A", null, null, ProjectComplexity.Small, 2026, null, null);
        var projectB = Project.Create("Proyecto B", null, null, ProjectComplexity.Small, 2026, null, null);
        db.Persons.Add(gestor);
        db.Projects.AddRange(projectA, projectB);
        await db.SaveChangesAsync();
        return (gestor, projectA, projectB);
    }

    // ─── CreateProjectDependency ──────────────────────────────────────────────

    [Fact]
    public async Task CreateDependency_ValidPair_CreatesSuccessfully()
    {
        await using var db = CreateDb();
        var (gestor, projA, projB) = await SeedTwoProjectsAsync(db);

        var handler = new CreateProjectDependencyHandler(db);
        var id = await handler.Handle(
            new CreateProjectDependencyCommand(projA.Id, projB.Id, "A depende de B", gestor.Id),
            CancellationToken.None);

        id.ShouldBeGreaterThan(0);
        var dep = await db.ProjectDependencies.FindAsync(id);
        dep.ShouldNotBeNull();
        dep.ProjectId.ShouldBe(projA.Id);
        dep.DependsOnProjectId.ShouldBe(projB.Id);
    }

    [Fact]
    public async Task CreateDependency_SelfDependency_ThrowsInvalidOperation()
    {
        await using var db = CreateDb();
        var (gestor, projA, _) = await SeedTwoProjectsAsync(db);

        var handler = new CreateProjectDependencyHandler(db);
        // La validación de auto-dependencia la hace el validator FluentValidation,
        // pero también podemos verificar el validator directamente
        var validator = new CreateProjectDependencyValidator();
        var cmd = new CreateProjectDependencyCommand(projA.Id, projA.Id, null, gestor.Id);
        var result = await validator.ValidateAsync(cmd);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "DependsOnProjectId");
    }

    [Fact]
    public async Task CreateDependency_DuplicatePair_ThrowsInvalidOperation()
    {
        await using var db = CreateDb();
        var (gestor, projA, projB) = await SeedTwoProjectsAsync(db);

        var handler = new CreateProjectDependencyHandler(db);
        await handler.Handle(
            new CreateProjectDependencyCommand(projA.Id, projB.Id, null, gestor.Id),
            CancellationToken.None);

        await Should.ThrowAsync<InvalidOperationException>(
            () => handler.Handle(
                new CreateProjectDependencyCommand(projA.Id, projB.Id, null, gestor.Id),
                CancellationToken.None));
    }

    [Fact]
    public async Task CreateDependency_CycleDirecto_ThrowsInvalidOperation()
    {
        await using var db = CreateDb();
        var (gestor, projA, projB) = await SeedTwoProjectsAsync(db);

        var handler = new CreateProjectDependencyHandler(db);
        // B depende de A
        await handler.Handle(
            new CreateProjectDependencyCommand(projB.Id, projA.Id, null, gestor.Id),
            CancellationToken.None);

        // Intentar que A dependa de B → ciclo directo
        var ex = await Should.ThrowAsync<InvalidOperationException>(
            () => handler.Handle(
                new CreateProjectDependencyCommand(projA.Id, projB.Id, null, gestor.Id),
                CancellationToken.None));

        ex.Message.ShouldContain("ciclo");
    }

    [Fact]
    public async Task CreateDependency_ProjectNotFound_ThrowsKeyNotFound()
    {
        await using var db = CreateDb();
        var (gestor, projA, _) = await SeedTwoProjectsAsync(db);

        var handler = new CreateProjectDependencyHandler(db);
        await Should.ThrowAsync<KeyNotFoundException>(
            () => handler.Handle(
                new CreateProjectDependencyCommand(projA.Id, 999, null, gestor.Id),
                CancellationToken.None));
    }

    [Fact]
    public async Task CreateDependency_Desarrollador_ThrowsUnauthorized()
    {
        await using var db = CreateDb();
        var dev = Person.CreateFromClaims("sub-dev", "Dev", "dev@test.com", PersonRole.Desarrollador);
        var projA = Project.Create("A", null, null, ProjectComplexity.VerySmall, null, null, null);
        var projB = Project.Create("B", null, null, ProjectComplexity.VerySmall, null, null, null);
        db.Persons.Add(dev);
        db.Projects.AddRange(projA, projB);
        await db.SaveChangesAsync();

        var handler = new CreateProjectDependencyHandler(db);
        await Should.ThrowAsync<UnauthorizedAccessException>(
            () => handler.Handle(
                new CreateProjectDependencyCommand(projA.Id, projB.Id, null, dev.Id),
                CancellationToken.None));
    }

    // ─── GetProjectDependencies ───────────────────────────────────────────────

    [Fact]
    public async Task GetDependencies_ReturnsBothDirections()
    {
        await using var db = CreateDb();
        var (gestor, projA, projB) = await SeedTwoProjectsAsync(db);
        var projC = Project.Create("Proyecto C", null, null, ProjectComplexity.Small, 2026, null, null);
        db.Projects.Add(projC);
        await db.SaveChangesAsync();

        // A depende de B; C depende de A
        db.ProjectDependencies.Add(ProjectDependency.Create(projA.Id, projB.Id, "A→B"));
        db.ProjectDependencies.Add(ProjectDependency.Create(projC.Id, projA.Id, "C→A"));
        await db.SaveChangesAsync();

        var handler = new GetProjectDependenciesHandler(db);
        var result = await handler.Handle(new GetProjectDependenciesQuery(projA.Id), CancellationToken.None);

        // A depende de B → DependsOn debe incluir B
        result.DependsOn.Count.ShouldBe(1);
        result.DependsOn[0].ProjectId.ShouldBe(projB.Id);
        result.DependsOn[0].ProjectTitle.ShouldBe("Proyecto B");

        // C depende de A → Dependents debe incluir C
        result.Dependents.Count.ShouldBe(1);
        result.Dependents[0].ProjectId.ShouldBe(projC.Id);
        result.Dependents[0].ProjectTitle.ShouldBe("Proyecto C");
    }

    [Fact]
    public async Task GetDependencies_NoDependencies_ReturnEmptyLists()
    {
        await using var db = CreateDb();
        var (_, projA, _) = await SeedTwoProjectsAsync(db);

        var handler = new GetProjectDependenciesHandler(db);
        var result = await handler.Handle(new GetProjectDependenciesQuery(projA.Id), CancellationToken.None);

        result.DependsOn.ShouldBeEmpty();
        result.Dependents.ShouldBeEmpty();
    }

    // ─── DeleteProjectDependency ──────────────────────────────────────────────

    [Fact]
    public async Task DeleteDependency_Gestor_DeletesSuccessfully()
    {
        await using var db = CreateDb();
        var (gestor, projA, projB) = await SeedTwoProjectsAsync(db);
        var dep = ProjectDependency.Create(projA.Id, projB.Id, null);
        db.ProjectDependencies.Add(dep);
        await db.SaveChangesAsync();

        var handler = new DeleteProjectDependencyHandler(db);
        await handler.Handle(new DeleteProjectDependencyCommand(projA.Id, dep.Id, gestor.Id), CancellationToken.None);

        (await db.ProjectDependencies.FindAsync(dep.Id)).ShouldBeNull();
    }

    [Fact]
    public async Task DeleteDependency_NotFound_ThrowsKeyNotFound()
    {
        await using var db = CreateDb();
        var (gestor, projA, _) = await SeedTwoProjectsAsync(db);

        var handler = new DeleteProjectDependencyHandler(db);
        await Should.ThrowAsync<KeyNotFoundException>(
            () => handler.Handle(
                new DeleteProjectDependencyCommand(projA.Id, 999, gestor.Id),
                CancellationToken.None));
    }
}
