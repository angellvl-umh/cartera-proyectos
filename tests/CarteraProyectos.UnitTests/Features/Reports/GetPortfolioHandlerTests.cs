using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Features.Reports;
using CarteraProyectos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace CarteraProyectos.UnitTests.Features.Reports;

public class GetPortfolioHandlerTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    // ─── PromoterId field (task 1.8) ──────────────────────────────────────────

    [Fact]
    public async Task GetPortfolio_ProjectWithPromoter_IncludesPromoterId()
    {
        await using var db = CreateDb();

        var promoter = Promoter.Create("Vicerrectorado");
        db.Promoters.Add(promoter);
        await db.SaveChangesAsync();

        db.Projects.Add(Project.Create("Proyecto 10", null, null, ProjectComplexity.Small, null, null, null, promoterId: promoter.Id));
        await db.SaveChangesAsync();

        var handler = new GetPortfolioHandler(db);
        var result = await handler.Handle(new GetPortfolioQuery(), CancellationToken.None);

        result.Projects.Count.ShouldBe(1);
        result.Projects[0].PromoterId.ShouldBe(promoter.Id);
    }

    [Fact]
    public async Task GetPortfolio_ProjectWithoutPromoter_PromoterIdIsNull()
    {
        await using var db = CreateDb();

        db.Projects.Add(Project.Create("Sin promotor", null, null, ProjectComplexity.Small, null, null, null));
        await db.SaveChangesAsync();

        var handler = new GetPortfolioHandler(db);
        var result = await handler.Handle(new GetPortfolioQuery(), CancellationToken.None);

        result.Projects.Count.ShouldBe(1);
        result.Projects[0].PromoterId.ShouldBeNull();
    }

    // ─── TagIds field (task 1.8) ──────────────────────────────────────────────

    [Fact]
    public async Task GetPortfolio_ProjectWithTags_IncludesTagIds()
    {
        await using var db = CreateDb();

        var tag1 = Tag.Create("Urgente", "#ff0000");
        var tag2 = Tag.Create("Innovación", "#00ff00");
        db.Tags.AddRange(tag1, tag2);
        await db.SaveChangesAsync();

        var project = Project.Create("Con etiquetas", null, null, ProjectComplexity.Small, null, null, null);
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        // Add tags via the many-to-many join directly through the project
        project.Tags.Add(tag1);
        project.Tags.Add(tag2);
        await db.SaveChangesAsync();

        var handler = new GetPortfolioHandler(db);
        var result = await handler.Handle(new GetPortfolioQuery(), CancellationToken.None);

        result.Projects.Count.ShouldBe(1);
        result.Projects[0].TagIds.ShouldContain(tag1.Id);
        result.Projects[0].TagIds.ShouldContain(tag2.Id);
        result.Projects[0].TagIds.Count.ShouldBe(2);
    }

    [Fact]
    public async Task GetPortfolio_ProjectWithoutTags_TagIdsIsEmptyList()
    {
        await using var db = CreateDb();

        db.Projects.Add(Project.Create("Sin etiquetas", null, null, ProjectComplexity.Small, null, null, null));
        await db.SaveChangesAsync();

        var handler = new GetPortfolioHandler(db);
        var result = await handler.Handle(new GetPortfolioQuery(), CancellationToken.None);

        result.Projects.Count.ShouldBe(1);
        result.Projects[0].TagIds.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetPortfolio_MultipleProjects_EachHasCorrectTagIds()
    {
        await using var db = CreateDb();

        var tag1 = Tag.Create("Tag1", null);
        var tag2 = Tag.Create("Tag2", null);
        db.Tags.AddRange(tag1, tag2);
        await db.SaveChangesAsync();

        var projectA = Project.Create("Proyecto A", null, null, ProjectComplexity.Small, null, null, null);
        var projectB = Project.Create("Proyecto B", null, null, ProjectComplexity.Small, null, null, null);
        db.Projects.AddRange(projectA, projectB);
        await db.SaveChangesAsync();

        projectA.Tags.Add(tag1);
        projectB.Tags.Add(tag2);
        await db.SaveChangesAsync();

        var handler = new GetPortfolioHandler(db);
        var result = await handler.Handle(new GetPortfolioQuery(), CancellationToken.None);

        result.Projects.Count.ShouldBe(2);
        var dtoA = result.Projects.First(p => p.Title == "Proyecto A");
        var dtoB = result.Projects.First(p => p.Title == "Proyecto B");
        dtoA.TagIds.ShouldBe([tag1.Id]);
        dtoB.TagIds.ShouldBe([tag2.Id]);
    }
}
