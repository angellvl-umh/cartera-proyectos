using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Features.Projects;
using CarteraProyectos.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace CarteraProyectos.UnitTests.Features.Projects;

public class BusinessValueHandlerTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    // ─── CreateProject ────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateProject_WithValidBusinessValue_StoresIt()
    {
        await using var db = CreateDb();
        var handler = new CreateProjectHandler(new ProjectLifecycleService(db));

        var id = await handler.Handle(
            new CreateProjectCommand("Portal TIC", null, null, ProjectComplexity.Small, 2026, null, null,
                BusinessValue: 4),
            CancellationToken.None);

        var project = await db.Projects.FindAsync(id);
        project.ShouldNotBeNull();
        project.BusinessValue.ShouldBe(4);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public async Task CreateProject_BusinessValue_BoundaryValues_AreValid(int value)
    {
        await using var db = CreateDb();
        var validator = new CreateProjectValidator();

        var cmd = new CreateProjectCommand("T", null, null, ProjectComplexity.VerySmall, null, null, null,
            BusinessValue: value);

        var result = await validator.ValidateAsync(cmd);
        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-1)]
    public async Task CreateProject_BusinessValue_OutOfRange_FailsValidation(int invalidValue)
    {
        var validator = new CreateProjectValidator();
        var cmd = new CreateProjectCommand("T", null, null, ProjectComplexity.VerySmall, null, null, null,
            BusinessValue: invalidValue);

        var result = await validator.ValidateAsync(cmd);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "BusinessValue");
    }

    [Fact]
    public async Task CreateProject_BusinessValue_Null_PassesValidation()
    {
        var validator = new CreateProjectValidator();
        var cmd = new CreateProjectCommand("T", null, null, ProjectComplexity.VerySmall, null, null, null,
            BusinessValue: null);

        var result = await validator.ValidateAsync(cmd);
        result.IsValid.ShouldBeTrue();
    }

    // ─── UpdateProject ────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateProject_WithValidBusinessValue_UpdatesIt()
    {
        await using var db = CreateDb();
        var project = Project.Create("Old", null, null, ProjectComplexity.VerySmall, null, null, null);
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        var handler = new UpdateProjectHandler(new ProjectLifecycleService(db));
        await handler.Handle(
            new UpdateProjectCommand(project.Id, "New", null, null, ProjectComplexity.VerySmall, null, null, null,
                BusinessValue: 2),
            CancellationToken.None);

        var updated = await db.Projects.FindAsync(project.Id);
        updated!.BusinessValue.ShouldBe(2);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public async Task UpdateProject_BusinessValue_OutOfRange_FailsValidation(int invalidValue)
    {
        var validator = new UpdateProjectValidator();
        var cmd = new UpdateProjectCommand(1, "T", null, null, ProjectComplexity.VerySmall, null, null, null,
            BusinessValue: invalidValue);

        var result = await validator.ValidateAsync(cmd);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "BusinessValue");
    }
}
