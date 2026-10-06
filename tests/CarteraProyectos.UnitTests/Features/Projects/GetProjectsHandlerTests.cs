using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Features.Projects;
using CarteraProyectos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace CarteraProyectos.UnitTests.Features.Projects;

public class GetProjectsHandlerTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    // ─── helpers ─────────────────────────────────────────────────────────────

    private static Promoter MakePromoter(string name)
        => Promoter.Create(name);

    private static OrganicUnit MakeOrganicUnit(string name, string? code = null)
        => OrganicUnit.Create(name, code);

    // ─── PromoterId filter (task 1.5) ─────────────────────────────────────────

    [Fact]
    public async Task GetProjects_FilterByPromoterId_ReturnsOnlyMatchingProjects()
    {
        await using var db = CreateDb();

        var promoter3 = MakePromoter("Promotor A");
        var promoter4 = MakePromoter("Promotor B");
        db.Promoters.AddRange(promoter3, promoter4);
        await db.SaveChangesAsync();

        var p10 = Project.Create("Proyecto 10", null, null, ProjectComplexity.Small, null, null, null, promoterId: promoter3.Id);
        var p11 = Project.Create("Proyecto 11", null, null, ProjectComplexity.Small, null, null, null, promoterId: promoter3.Id);
        var p12 = Project.Create("Proyecto 12", null, null, ProjectComplexity.Small, null, null, null, promoterId: promoter4.Id);
        db.Projects.AddRange(p10, p11, p12);
        await db.SaveChangesAsync();

        var handler = new GetProjectsHandler(db);
        var result = await handler.Handle(new GetProjectsQuery(PromoterId: promoter3.Id), CancellationToken.None);

        result.Total.ShouldBe(2);
        result.Items.ShouldAllBe(p => p.PromoterId == promoter3.Id);
        result.Items.Select(p => p.Title).ShouldContain("Proyecto 10");
        result.Items.Select(p => p.Title).ShouldContain("Proyecto 11");
    }

    [Fact]
    public async Task GetProjects_FilterByUnknownPromoterId_ReturnsEmpty()
    {
        await using var db = CreateDb();

        db.Projects.Add(Project.Create("Proyecto X", null, null, ProjectComplexity.Small, null, null, null));
        await db.SaveChangesAsync();

        var handler = new GetProjectsHandler(db);
        var result = await handler.Handle(new GetProjectsQuery(PromoterId: 999), CancellationToken.None);

        result.Total.ShouldBe(0);
        result.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetProjects_FilterByPromoterId_ExcludesProjectsWithoutPromoter()
    {
        await using var db = CreateDb();

        var promoter = MakePromoter("Promotor C");
        db.Promoters.Add(promoter);
        await db.SaveChangesAsync();

        var withPromoter = Project.Create("Con promotor", null, null, ProjectComplexity.Small, null, null, null, promoterId: promoter.Id);
        var withoutPromoter = Project.Create("Sin promotor", null, null, ProjectComplexity.Small, null, null, null);
        db.Projects.AddRange(withPromoter, withoutPromoter);
        await db.SaveChangesAsync();

        var handler = new GetProjectsHandler(db);
        var result = await handler.Handle(new GetProjectsQuery(PromoterId: promoter.Id), CancellationToken.None);

        result.Total.ShouldBe(1);
        result.Items[0].Title.ShouldBe("Con promotor");
    }

    [Fact]
    public async Task GetProjects_FilterByPromoterIdAndStatus_CombinesFilters()
    {
        await using var db = CreateDb();

        var promoter = MakePromoter("Promotor D");
        db.Promoters.Add(promoter);
        await db.SaveChangesAsync();

        var p1 = Project.Create("Portal", null, null, ProjectComplexity.Small, null, null, null, promoterId: promoter.Id);
        p1.TransitionTo(ProjectStatus.PlanningWithClient);
        p1.TransitionTo(ProjectStatus.PlanningSprint);
        p1.TransitionTo(ProjectStatus.InSprint);

        var p2 = Project.Create("Portal 2", null, null, ProjectComplexity.Small, null, null, null, promoterId: promoter.Id);
        // p2 stays Stopped

        var p3 = Project.Create("Otro", null, null, ProjectComplexity.Small, null, null, null);
        p3.TransitionTo(ProjectStatus.PlanningWithClient);
        p3.TransitionTo(ProjectStatus.PlanningSprint);
        p3.TransitionTo(ProjectStatus.InSprint);

        db.Projects.AddRange(p1, p2, p3);
        await db.SaveChangesAsync();

        var handler = new GetProjectsHandler(db);
        var result = await handler.Handle(new GetProjectsQuery(PromoterId: promoter.Id, Status: ProjectStatus.InSprint), CancellationToken.None);

        result.Total.ShouldBe(1);
        result.Items[0].Title.ShouldBe("Portal");
    }

    [Fact]
    public async Task GetProjects_FilterByPromoterId_TotalReflectsFilteredCount()
    {
        await using var db = CreateDb();

        var promoter = MakePromoter("Promotor E");
        db.Promoters.Add(promoter);
        await db.SaveChangesAsync();

        // Add 3 projects for promoter, 2 for others
        for (var i = 0; i < 3; i++)
            db.Projects.Add(Project.Create($"P{i}", null, null, ProjectComplexity.Small, null, null, null, promoterId: promoter.Id));
        for (var i = 0; i < 2; i++)
            db.Projects.Add(Project.Create($"Other{i}", null, null, ProjectComplexity.Small, null, null, null));
        await db.SaveChangesAsync();

        var handler = new GetProjectsHandler(db);
        var result = await handler.Handle(new GetProjectsQuery(PromoterId: promoter.Id, Page: 1, PageSize: 1), CancellationToken.None);

        result.Total.ShouldBe(3);
        result.Items.Count.ShouldBe(1);
        result.Page.ShouldBe(1);
    }

    [Fact]
    public async Task GetProjects_FilterByPromoterId_PaginationWorks()
    {
        await using var db = CreateDb();

        var promoter = MakePromoter("Promotor F");
        db.Promoters.Add(promoter);
        await db.SaveChangesAsync();

        db.Projects.Add(Project.Create("Proyecto Alfa", null, null, ProjectComplexity.Small, null, null, null, promoterId: promoter.Id));
        db.Projects.Add(Project.Create("Proyecto Beta", null, null, ProjectComplexity.Small, null, null, null, promoterId: promoter.Id));
        await db.SaveChangesAsync();

        var handler = new GetProjectsHandler(db);
        var result = await handler.Handle(new GetProjectsQuery(PromoterId: promoter.Id, Page: 2, PageSize: 1), CancellationToken.None);

        result.Total.ShouldBe(2);
        result.Items.Count.ShouldBe(1);
        result.Page.ShouldBe(2);
    }

    // ─── OrganicUnit fields (task 1.6) ────────────────────────────────────────

    [Fact]
    public async Task GetProjects_WithOrganicUnit_ReturnsOrganicUnitFields()
    {
        await using var db = CreateDb();

        var ou = MakeOrganicUnit("Servicio de Informática");
        db.OrganicUnits.Add(ou);
        await db.SaveChangesAsync();

        db.Projects.Add(Project.Create("Proyecto con UO", null, null, ProjectComplexity.Small, null, null, null, organicUnitId: ou.Id));
        await db.SaveChangesAsync();

        var handler = new GetProjectsHandler(db);
        var result = await handler.Handle(new GetProjectsQuery(), CancellationToken.None);

        result.Items.Count.ShouldBe(1);
        result.Items[0].OrganicUnitId.ShouldBe(ou.Id);
        result.Items[0].OrganicUnitName.ShouldBe("Servicio de Informática");
    }

    [Fact]
    public async Task GetProjects_WithoutOrganicUnit_OrganicUnitFieldsAreNull()
    {
        await using var db = CreateDb();

        db.Projects.Add(Project.Create("Sin UO", null, null, ProjectComplexity.Small, null, null, null));
        await db.SaveChangesAsync();

        var handler = new GetProjectsHandler(db);
        var result = await handler.Handle(new GetProjectsQuery(), CancellationToken.None);

        result.Items.Count.ShouldBe(1);
        result.Items[0].OrganicUnitId.ShouldBeNull();
        result.Items[0].OrganicUnitName.ShouldBeNull();
    }

    // ─── Sorting (task 1.7) ────────────────────────────────────────────────────

    [Fact]
    public async Task GetProjects_SortByTitle_Ascending()
    {
        await using var db = CreateDb();

        db.Projects.AddRange(
            Project.Create("Zebra", null, null, ProjectComplexity.Small, null, null, null),
            Project.Create("Alpha", null, null, ProjectComplexity.Small, null, null, null),
            Project.Create("Mango", null, null, ProjectComplexity.Small, null, null, null));
        await db.SaveChangesAsync();

        var handler = new GetProjectsHandler(db);
        var result = await handler.Handle(new GetProjectsQuery(SortBy: "title", SortDir: "asc"), CancellationToken.None);

        result.Items.Select(p => p.Title).ShouldBe(["Alpha", "Mango", "Zebra"]);
    }

    [Fact]
    public async Task GetProjects_SortByTitle_Descending()
    {
        await using var db = CreateDb();

        db.Projects.AddRange(
            Project.Create("Zebra", null, null, ProjectComplexity.Small, null, null, null),
            Project.Create("Alpha", null, null, ProjectComplexity.Small, null, null, null),
            Project.Create("Mango", null, null, ProjectComplexity.Small, null, null, null));
        await db.SaveChangesAsync();

        var handler = new GetProjectsHandler(db);
        var result = await handler.Handle(new GetProjectsQuery(SortBy: "title", SortDir: "desc"), CancellationToken.None);

        result.Items.Select(p => p.Title).ShouldBe(["Zebra", "Mango", "Alpha"]);
    }

    [Fact]
    public async Task GetProjects_SortByPromoter_NullsLast()
    {
        await using var db = CreateDb();

        var p1 = MakePromoter("Biblioteca");
        var p2 = MakePromoter("Alumnos");
        db.Promoters.AddRange(p1, p2);
        await db.SaveChangesAsync();

        db.Projects.AddRange(
            Project.Create("Con Biblioteca", null, null, ProjectComplexity.Small, null, null, null, promoterId: p1.Id),
            Project.Create("Con Alumnos", null, null, ProjectComplexity.Small, null, null, null, promoterId: p2.Id),
            Project.Create("Sin promotor", null, null, ProjectComplexity.Small, null, null, null));
        await db.SaveChangesAsync();

        var handler = new GetProjectsHandler(db);
        var result = await handler.Handle(new GetProjectsQuery(SortBy: "promoter", SortDir: "asc"), CancellationToken.None);

        result.Items[0].PromoterName.ShouldBe("Alumnos");
        result.Items[1].PromoterName.ShouldBe("Biblioteca");
        result.Items[2].PromoterName.ShouldBeNull(); // null last
    }

    [Fact]
    public async Task GetProjects_SortByOrganicUnit_Descending_NullsLast()
    {
        await using var db = CreateDb();

        var ou1 = MakeOrganicUnit("Informática");
        var ou2 = MakeOrganicUnit("Administración");
        db.OrganicUnits.AddRange(ou1, ou2);
        await db.SaveChangesAsync();

        db.Projects.AddRange(
            Project.Create("Con Informática", null, null, ProjectComplexity.Small, null, null, null, organicUnitId: ou1.Id),
            Project.Create("Con Administración", null, null, ProjectComplexity.Small, null, null, null, organicUnitId: ou2.Id),
            Project.Create("Sin UO", null, null, ProjectComplexity.Small, null, null, null));
        await db.SaveChangesAsync();

        var handler = new GetProjectsHandler(db);
        var result = await handler.Handle(new GetProjectsQuery(SortBy: "organicUnit", SortDir: "desc"), CancellationToken.None);

        result.Items[0].OrganicUnitName.ShouldBe("Informática");
        result.Items[1].OrganicUnitName.ShouldBe("Administración");
        result.Items[2].OrganicUnitName.ShouldBeNull(); // null last
    }

    [Fact]
    public async Task GetProjects_SortByComplexity_NaturalOrder()
    {
        await using var db = CreateDb();

        db.Projects.AddRange(
            Project.Create("Grande", null, null, ProjectComplexity.Large, null, null, null),
            Project.Create("MuyPequeño", null, null, ProjectComplexity.VerySmall, null, null, null),
            Project.Create("Medio", null, null, ProjectComplexity.Medium, null, null, null));
        await db.SaveChangesAsync();

        var handler = new GetProjectsHandler(db);
        var result = await handler.Handle(new GetProjectsQuery(SortBy: "complexity", SortDir: "asc"), CancellationToken.None);

        result.Items[0].Complexity.ShouldBe("VerySmall");
        result.Items[1].Complexity.ShouldBe("Medium");
        result.Items[2].Complexity.ShouldBe("Large");
    }

    [Fact]
    public async Task GetProjects_SortByPortfolioYear_Ascending()
    {
        await using var db = CreateDb();

        db.Projects.AddRange(
            Project.Create("P2026", null, null, ProjectComplexity.Small, 2026, null, null),
            Project.Create("P2024", null, null, ProjectComplexity.Small, 2024, null, null),
            Project.Create("Sin año", null, null, ProjectComplexity.Small, null, null, null));
        await db.SaveChangesAsync();

        var handler = new GetProjectsHandler(db);
        var result = await handler.Handle(new GetProjectsQuery(SortBy: "portfolioYear", SortDir: "asc"), CancellationToken.None);

        result.Items[0].PortfolioYear.ShouldBe(2024);
        result.Items[1].PortfolioYear.ShouldBe(2026);
        result.Items[2].PortfolioYear.ShouldBeNull(); // null last
    }

    [Fact]
    public async Task GetProjects_SortByPortfolioYear_TiesOrderedByIdDescending()
    {
        await using var db = CreateDb();

        var p1 = Project.Create("Primero", null, null, ProjectComplexity.Small, 2026, null, null);
        var p2 = Project.Create("Segundo", null, null, ProjectComplexity.Small, 2026, null, null);
        db.Projects.AddRange(p1, p2);
        await db.SaveChangesAsync();

        var handler = new GetProjectsHandler(db);
        var result = await handler.Handle(new GetProjectsQuery(SortBy: "portfolioYear", SortDir: "asc"), CancellationToken.None);

        // Both have same year; higher Id comes first (stable tiebreak)
        result.Items[0].Id.ShouldBeGreaterThan(result.Items[1].Id);
    }

    [Fact]
    public async Task GetProjects_UnknownSortBy_UsesDefaultOrder()
    {
        await using var db = CreateDb();

        var p1 = Project.Create("A", null, null, ProjectComplexity.Small, null, null, null);
        var p2 = Project.Create("B", null, null, ProjectComplexity.Small, null, null, null);
        db.Projects.AddRange(p1, p2);
        await db.SaveChangesAsync();

        var handler = new GetProjectsHandler(db);
        var result = await handler.Handle(new GetProjectsQuery(SortBy: "foo", SortDir: "bar"), CancellationToken.None);

        // Default order: Id descending
        result.Items[0].Id.ShouldBeGreaterThan(result.Items[1].Id);
    }

    [Fact]
    public async Task GetProjects_SortAppliesAcrossPages()
    {
        await using var db = CreateDb();

        db.Projects.AddRange(
            Project.Create("D", null, null, ProjectComplexity.Small, null, null, null),
            Project.Create("C", null, null, ProjectComplexity.Small, null, null, null),
            Project.Create("B", null, null, ProjectComplexity.Small, null, null, null),
            Project.Create("A", null, null, ProjectComplexity.Small, null, null, null));
        await db.SaveChangesAsync();

        var handler = new GetProjectsHandler(db);
        var page2 = await handler.Handle(new GetProjectsQuery(SortBy: "title", SortDir: "asc", Page: 2, PageSize: 2), CancellationToken.None);

        page2.Items.Select(p => p.Title).ShouldBe(["C", "D"]);
    }

    [Fact]
    public async Task GetProjects_SortByPromoterCombinedWithFilter()
    {
        await using var db = CreateDb();

        var promoter = MakePromoter("Vicerrectorado");
        db.Promoters.Add(promoter);
        await db.SaveChangesAsync();

        db.Projects.AddRange(
            Project.Create("Zebra", null, null, ProjectComplexity.Small, null, null, null, promoterId: promoter.Id),
            Project.Create("Alpha", null, null, ProjectComplexity.Small, null, null, null, promoterId: promoter.Id),
            Project.Create("Otro", null, null, ProjectComplexity.Small, null, null, null));
        await db.SaveChangesAsync();

        var handler = new GetProjectsHandler(db);
        var result = await handler.Handle(new GetProjectsQuery(PromoterId: promoter.Id, SortBy: "title", SortDir: "asc"), CancellationToken.None);

        result.Total.ShouldBe(2);
        result.Items.Select(p => p.Title).ShouldBe(["Alpha", "Zebra"]);
    }

    [Fact]
    public async Task GetProjects_SortByComplexity_Descending()
    {
        await using var db = CreateDb();

        db.Projects.AddRange(
            Project.Create("MuyGrande", null, null, ProjectComplexity.VeryLarge, null, null, null),
            Project.Create("MuyPequeño", null, null, ProjectComplexity.VerySmall, null, null, null),
            Project.Create("Medio", null, null, ProjectComplexity.Medium, null, null, null));
        await db.SaveChangesAsync();

        var handler = new GetProjectsHandler(db);
        var result = await handler.Handle(new GetProjectsQuery(SortBy: "complexity", SortDir: "desc"), CancellationToken.None);

        result.Items[0].Complexity.ShouldBe("VeryLarge");
        result.Items[1].Complexity.ShouldBe("Medium");
        result.Items[2].Complexity.ShouldBe("VerySmall");
    }
}
