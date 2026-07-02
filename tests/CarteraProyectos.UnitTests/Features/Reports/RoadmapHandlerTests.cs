using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Features.Reports;
using CarteraProyectos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace CarteraProyectos.UnitTests.Features.Reports;

public class RoadmapHandlerTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    /// <summary>Crea un proyecto con PortfolioYear 2026 y StartDate en el año indicado.</summary>
    private static Project MakeProject(
        string title,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        ProjectStatus status = ProjectStatus.InSprint,
        ProjectComplexity complexity = ProjectComplexity.Small,
        int portfolioYear = 2026)
    {
        var p = Project.Create(
            title, null, "TIC", complexity,
            portfolioYear, startDate, endDate);
        AdvanceProjectTo(p, status);
        return p;
    }

    private static void AdvanceProjectTo(Project p, ProjectStatus target)
    {
        var path = new[]
        {
            ProjectStatus.Stopped,
            ProjectStatus.PlanningWithClient,
            ProjectStatus.PlanningSprint,
            ProjectStatus.InSprint,
            ProjectStatus.InTesting,
            ProjectStatus.Completed,
        };
        var idx = Array.IndexOf(path, target);
        for (var i = 1; i <= idx; i++)
            p.TransitionTo(path[i]);
    }

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Roadmap_ProjectWithPrimaryTeam_AppearsUnderThatTeam()
    {
        await using var db = CreateDb();

        var project = MakeProject("Proyecto Alfa",
            startDate: new DateOnly(2026, 3, 1),
            endDate: new DateOnly(2026, 6, 30));
        var team = Team.Create("Equipo Web", null, null);

        db.Projects.Add(project);
        db.Teams.Add(team);
        await db.SaveChangesAsync();

        db.ProjectTeamAssignments.Add(
            ProjectTeamAssignment.Create(project.Id, team.Id, isPrimary: true));
        await db.SaveChangesAsync();

        var handler = new GetPortfolioRoadmapHandler(db);
        var result = await handler.Handle(new GetPortfolioRoadmapQuery(2026), CancellationToken.None);

        result.Teams.Count.ShouldBe(1);
        result.Teams[0].TeamName.ShouldBe("Equipo Web");
        result.Teams[0].Projects.Count.ShouldBe(1);
        result.Teams[0].Projects[0].Title.ShouldBe("Proyecto Alfa");
        result.Unassigned.ShouldBeEmpty();
        result.Undated.ShouldBeEmpty();
    }

    [Fact]
    public async Task Roadmap_ProjectWithNoTeam_AppearsInUnassigned()
    {
        await using var db = CreateDb();

        var project = MakeProject("Sin Equipo",
            startDate: new DateOnly(2026, 2, 1),
            endDate: new DateOnly(2026, 5, 31));

        db.Projects.Add(project);
        await db.SaveChangesAsync();

        var handler = new GetPortfolioRoadmapHandler(db);
        var result = await handler.Handle(new GetPortfolioRoadmapQuery(2026), CancellationToken.None);

        result.Teams.ShouldBeEmpty();
        result.Unassigned.Count.ShouldBe(1);
        result.Unassigned[0].Title.ShouldBe("Sin Equipo");
        result.Undated.ShouldBeEmpty();
    }

    [Fact]
    public async Task Roadmap_ProjectWithoutStartDate_AppearsInUndated()
    {
        await using var db = CreateDb();

        // Proyecto con PortfolioYear 2026 pero sin StartDate
        var project = MakeProject("Sin Fecha", startDate: null, portfolioYear: 2026);

        db.Projects.Add(project);
        await db.SaveChangesAsync();

        var handler = new GetPortfolioRoadmapHandler(db);
        var result = await handler.Handle(new GetPortfolioRoadmapQuery(2026), CancellationToken.None);

        result.Teams.ShouldBeEmpty();
        result.Unassigned.ShouldBeEmpty();
        result.Undated.Count.ShouldBe(1);
        result.Undated[0].Title.ShouldBe("Sin Fecha");
    }

    [Fact]
    public async Task Roadmap_MilestoneReachedWhenDone_IsTrue()
    {
        await using var db = CreateDb();

        var project = MakeProject("Con Hitos",
            startDate: new DateOnly(2026, 1, 1),
            endDate: new DateOnly(2026, 12, 31));
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        // Hito Done → Reached=true
        var hitoReached = WorkItem.Create(project.Id, "Hito completado", null,
            WorkItemPriority.High, null, 0, null,
            isHito: true, hitoDate: new DateOnly(2026, 6, 1), dueDate: null);
        hitoReached.TransitionStatus(WorkItemStatus.Done);

        // Hito pendiente → Reached=false
        var hitoOpen = WorkItem.Create(project.Id, "Hito pendiente", null,
            WorkItemPriority.Medium, null, 1, null,
            isHito: true, hitoDate: new DateOnly(2026, 9, 1), dueDate: null);

        db.WorkItems.AddRange(hitoReached, hitoOpen);
        await db.SaveChangesAsync();

        var handler = new GetPortfolioRoadmapHandler(db);
        var result = await handler.Handle(new GetPortfolioRoadmapQuery(2026), CancellationToken.None);

        result.Unassigned.Count.ShouldBe(1);
        var milestones = result.Unassigned[0].Milestones;
        milestones.Count.ShouldBe(2);

        var reached = milestones.Single(m => m.Title == "Hito completado");
        reached.Reached.ShouldBeTrue();
        reached.HitoDate.ShouldBe("2026-06-01");

        var open = milestones.Single(m => m.Title == "Hito pendiente");
        open.Reached.ShouldBeFalse();
    }

    [Fact]
    public async Task Roadmap_ProjectFromDifferentYearWithoutOverlap_IsExcluded()
    {
        await using var db = CreateDb();

        // Proyecto de 2024 que no solapa con 2026
        var old = Project.Create("Viejo", null, "TIC", ProjectComplexity.Small,
            2024,
            startDate: new DateOnly(2024, 1, 1),
            endDate: new DateOnly(2024, 12, 31));

        // Proyecto de 2026
        var current = MakeProject("Actual",
            startDate: new DateOnly(2026, 1, 1),
            endDate: new DateOnly(2026, 6, 30));

        db.Projects.AddRange(old, current);
        await db.SaveChangesAsync();

        var handler = new GetPortfolioRoadmapHandler(db);
        var result = await handler.Handle(new GetPortfolioRoadmapQuery(2026), CancellationToken.None);

        // Solo el proyecto de 2026 debe aparecer
        var allProjects = result.Teams
            .SelectMany(t => t.Projects)
            .Concat(result.Unassigned)
            .Concat(result.Undated)
            .ToList();

        allProjects.Count.ShouldBe(1);
        allProjects[0].Title.ShouldBe("Actual");
    }

    [Fact]
    public async Task Roadmap_CompletedProjectEndedBeforeYear_IsExcluded()
    {
        await using var db = CreateDb();

        // Proyecto Completed que terminó en 2024 — no debe aparecer en 2026
        var completed = Project.Create("Completado Antiguo", null, "TIC",
            ProjectComplexity.Small, 2024,
            startDate: new DateOnly(2024, 1, 1),
            endDate: new DateOnly(2024, 12, 31));
        AdvanceProjectTo(completed, ProjectStatus.Completed);

        db.Projects.Add(completed);
        await db.SaveChangesAsync();

        var handler = new GetPortfolioRoadmapHandler(db);
        var result = await handler.Handle(new GetPortfolioRoadmapQuery(2026), CancellationToken.None);

        result.Teams.ShouldBeEmpty();
        result.Unassigned.ShouldBeEmpty();
        result.Undated.ShouldBeEmpty();
    }

    [Fact]
    public async Task Roadmap_ProjectOverlappingYearByDateRange_IsIncluded()
    {
        await using var db = CreateDb();

        // Proyecto que empieza en 2025 y termina en 2026: rango solapa 2026
        var crossYear = Project.Create("Multi-año", null, "TIC",
            ProjectComplexity.Medium, 2025,
            startDate: new DateOnly(2025, 10, 1),
            endDate: new DateOnly(2026, 3, 31));

        db.Projects.Add(crossYear);
        await db.SaveChangesAsync();

        var handler = new GetPortfolioRoadmapHandler(db);
        var result = await handler.Handle(new GetPortfolioRoadmapQuery(2026), CancellationToken.None);

        var allProjects = result.Teams
            .SelectMany(t => t.Projects)
            .Concat(result.Unassigned)
            .Concat(result.Undated)
            .ToList();

        allProjects.Count.ShouldBe(1);
        allProjects[0].Title.ShouldBe("Multi-año");
    }

    [Fact]
    public async Task Roadmap_NonPrimaryTeamUsedWhenNoPrimary()
    {
        await using var db = CreateDb();

        var project = MakeProject("Sin Primario",
            startDate: new DateOnly(2026, 1, 1),
            endDate: new DateOnly(2026, 6, 30));
        var team = Team.Create("Equipo Secundario", null, null);

        db.Projects.Add(project);
        db.Teams.Add(team);
        await db.SaveChangesAsync();

        // Asignamos con isPrimary=false
        db.ProjectTeamAssignments.Add(
            ProjectTeamAssignment.Create(project.Id, team.Id, isPrimary: false));
        await db.SaveChangesAsync();

        var handler = new GetPortfolioRoadmapHandler(db);
        var result = await handler.Handle(new GetPortfolioRoadmapQuery(2026), CancellationToken.None);

        // Debe aparecer bajo ese equipo (el único disponible), no en Unassigned
        result.Teams.Count.ShouldBe(1);
        result.Teams[0].TeamName.ShouldBe("Equipo Secundario");
        result.Unassigned.ShouldBeEmpty();
    }

    [Fact]
    public async Task Roadmap_TeamsOrderedAlphabetically()
    {
        await using var db = CreateDb();

        var pA = MakeProject("Proyecto Zeta", startDate: new DateOnly(2026, 1, 1));
        var pB = MakeProject("Proyecto Alpha", startDate: new DateOnly(2026, 2, 1));
        var tZ = Team.Create("Zebra Team", null, null);
        var tA = Team.Create("Alpha Team", null, null);

        db.Projects.AddRange(pA, pB);
        db.Teams.AddRange(tZ, tA);
        await db.SaveChangesAsync();

        db.ProjectTeamAssignments.Add(ProjectTeamAssignment.Create(pA.Id, tZ.Id, isPrimary: true));
        db.ProjectTeamAssignments.Add(ProjectTeamAssignment.Create(pB.Id, tA.Id, isPrimary: true));
        await db.SaveChangesAsync();

        var handler = new GetPortfolioRoadmapHandler(db);
        var result = await handler.Handle(new GetPortfolioRoadmapQuery(2026), CancellationToken.None);

        result.Teams[0].TeamName.ShouldBe("Alpha Team");
        result.Teams[1].TeamName.ShouldBe("Zebra Team");
    }

    [Fact]
    public async Task Roadmap_DefaultYearIsCurrentYear()
    {
        await using var db = CreateDb();

        var currentYear = DateTime.UtcNow.Year;
        var project = Project.Create("Este Año", null, "TIC",
            ProjectComplexity.Small, currentYear,
            startDate: new DateOnly(currentYear, 1, 1),
            endDate: new DateOnly(currentYear, 12, 31));

        db.Projects.Add(project);
        await db.SaveChangesAsync();

        var handler = new GetPortfolioRoadmapHandler(db);
        // Sin pasar Year → debe usar el año actual
        var result = await handler.Handle(new GetPortfolioRoadmapQuery(), CancellationToken.None);

        result.Year.ShouldBe(currentYear);
        var all = result.Teams.SelectMany(t => t.Projects)
            .Concat(result.Unassigned).Concat(result.Undated).ToList();
        all.Count.ShouldBe(1);
    }
}
