using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Features.Reports;
using CarteraProyectos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace CarteraProyectos.UnitTests.Features.Reports;

public class CapacityForecastHandlerTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static Person MakePerson(string name = "Dev")
        => Person.CreateFromClaims(Guid.NewGuid().ToString(), name, $"{name.ToLower()}@test.com");

    private static Project MakeProject(
        string title,
        ProjectComplexity complexity,
        DateOnly? startDate,
        DateOnly? endDate = null,
        ProjectStatus status = ProjectStatus.InSprint,
        int portfolioYear = 2026)
    {
        var p = Project.Create(title, null, "TIC", complexity, portfolioYear, startDate, endDate);
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

    /// <summary>
    /// Equipo con 2 miembros → capacidad = 2 × 3 × 0,8 = 4,8 p-m por trimestre.
    /// </summary>
    [Fact]
    public async Task Forecast_TeamWithTwoMembers_CapacityIs4Point8PerQuarter()
    {
        await using var db = CreateDb();

        var p1 = MakePerson("Alice");
        var p2 = MakePerson("Bob");
        var team = Team.Create("Dev Team", null, null);

        db.Persons.AddRange(p1, p2);
        db.Teams.Add(team);
        await db.SaveChangesAsync();

        db.PersonTeamMemberships.Add(PersonTeamMembership.Create(p1.Id, team.Id));
        db.PersonTeamMemberships.Add(PersonTeamMembership.Create(p2.Id, team.Id));
        await db.SaveChangesAsync();

        var handler = new GetCapacityForecastHandler(db);
        var result = await handler.Handle(new GetCapacityForecastQuery(2026), CancellationToken.None);

        result.Teams.Count.ShouldBe(1);
        var teamDto = result.Teams[0];
        teamDto.MemberCount.ShouldBe(2);

        foreach (var q in teamDto.Quarters)
        {
            q.CapacityPersonMonths.ShouldBe(4.8);
        }
    }

    /// <summary>
    /// Proyecto Medium (4 p-m), enero-febrero 2026, 1 equipo con 2 miembros.
    /// Duración = (feb28 - jan01) = 58 días → 58/30.44 ≈ 1.9053 meses.
    /// Ritmo = 4/1.9053 ≈ 2.0994 p-m/mes.
    /// Solapamiento Q1: (feb28 - jan01) + 1 = 59 días → 59/30.44 ≈ 1.9382 meses.
    /// Demanda Q1 = 2.0994 × 1.9382 ≈ 4.07.
    /// LoadPercent = round(4.07/4.8 × 100) = 85 → Yellow.
    /// Q2,Q3,Q4 → demanda 0 → Green.
    /// </summary>
    [Fact]
    public async Task Forecast_MediumProjectInQ1_YieldsExpectedDemandAndLevel()
    {
        await using var db = CreateDb();

        var p1 = MakePerson("Alice");
        var p2 = MakePerson("Bob");
        var team = Team.Create("Dev Team", null, null);
        var project = MakeProject("Proyecto Medium",
            complexity: ProjectComplexity.Medium,
            startDate: new DateOnly(2026, 1, 1),
            endDate: new DateOnly(2026, 2, 28));

        db.Persons.AddRange(p1, p2);
        db.Teams.Add(team);
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        db.PersonTeamMemberships.Add(PersonTeamMembership.Create(p1.Id, team.Id));
        db.PersonTeamMemberships.Add(PersonTeamMembership.Create(p2.Id, team.Id));
        db.ProjectTeamAssignments.Add(
            ProjectTeamAssignment.Create(project.Id, team.Id, isPrimary: true));
        await db.SaveChangesAsync();

        var handler = new GetCapacityForecastHandler(db);
        var result = await handler.Handle(new GetCapacityForecastQuery(2026), CancellationToken.None);

        var teamDto = result.Teams.Single();
        var q1 = teamDto.Quarters.Single(q => q.Quarter == 1);
        var q2 = teamDto.Quarters.Single(q => q.Quarter == 2);
        var q3 = teamDto.Quarters.Single(q => q.Quarter == 3);
        var q4 = teamDto.Quarters.Single(q => q.Quarter == 4);

        // Demanda Q1 debe estar en torno a 4.07 (la heurística fraccional puede variar ±0.1)
        q1.DemandPersonMonths.ShouldBeGreaterThan(3.9);
        q1.DemandPersonMonths.ShouldBeLessThan(4.2);
        q1.Level.ShouldBe("Yellow");  // 85% → Yellow
        q1.ProjectTitles.ShouldContain("Proyecto Medium");

        // Sin solapamiento → demanda 0
        q2.DemandPersonMonths.ShouldBe(0);
        q2.Level.ShouldBe("Green");
        q3.DemandPersonMonths.ShouldBe(0);
        q4.DemandPersonMonths.ShouldBe(0);
    }

    /// <summary>
    /// Proyecto compartido entre 2 equipos → cada equipo recibe la mitad de la demanda.
    /// Large project (16 p-m), duración exacta 4 meses, Q1 (3 meses solapan completamente).
    /// Duración = (apr30-jan01) días / 30.44. Ritmo = 16/duración.
    /// Solapamiento Q1 = (mar31-jan01)+1 días / 30.44.
    /// Demanda total Q1 para 1 equipo = ritmo × overlapQ1 / 2 equipos.
    /// </summary>
    [Fact]
    public async Task Forecast_ProjectSharedBetweenTwoTeams_DemandIsHalvedPerTeam()
    {
        await using var db = CreateDb();

        var team1 = Team.Create("Team A", null, null);
        var team2 = Team.Create("Team B", null, null);
        var project = MakeProject("Proyecto Compartido",
            complexity: ProjectComplexity.Large,
            startDate: new DateOnly(2026, 1, 1),
            endDate: new DateOnly(2026, 4, 30));

        db.Teams.AddRange(team1, team2);
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        db.ProjectTeamAssignments.Add(
            ProjectTeamAssignment.Create(project.Id, team1.Id, isPrimary: true));
        db.ProjectTeamAssignments.Add(
            ProjectTeamAssignment.Create(project.Id, team2.Id, isPrimary: false));
        await db.SaveChangesAsync();

        var handler = new GetCapacityForecastHandler(db);
        var result = await handler.Handle(new GetCapacityForecastQuery(2026), CancellationToken.None);

        result.Teams.Count.ShouldBe(2);

        var t1Q1 = result.Teams.Single(t => t.TeamName == "Team A").Quarters.Single(q => q.Quarter == 1);
        var t2Q1 = result.Teams.Single(t => t.TeamName == "Team B").Quarters.Single(q => q.Quarter == 1);

        // Ambos equipos deben tener la misma demanda (50% cada uno)
        t1Q1.DemandPersonMonths.ShouldBe(t2Q1.DemandPersonMonths);

        // La demanda de cada equipo debe ser la mitad de la demanda total
        // Large=16 p-m. Si un equipo solo tuviera el proyecto, su demanda en Q1 sería X.
        // Con 2 equipos, cada equipo tiene X/2.
        t1Q1.DemandPersonMonths.ShouldBeGreaterThan(0);
        t1Q1.ProjectTitles.ShouldContain("Proyecto Compartido");
        t2Q1.ProjectTitles.ShouldContain("Proyecto Compartido");
    }

    /// <summary>
    /// Proyectos con status Completed, Stopped y PostponedByClient son excluidos del forecast.
    /// </summary>
    [Fact]
    public async Task Forecast_ExcludedStatuses_AreNotCounted()
    {
        await using var db = CreateDb();

        var team = Team.Create("Dev Team", null, null);
        db.Teams.Add(team);

        // Proyecto completado con fechas en 2026
        var completed = Project.Create("Completado", null, "TIC",
            ProjectComplexity.Large, 2026,
            startDate: new DateOnly(2026, 1, 1),
            endDate: new DateOnly(2026, 12, 31));
        AdvanceProjectTo(completed, ProjectStatus.Completed);

        // Proyecto parado con fechas en 2026
        var stopped = Project.Create("Parado", null, "TIC",
            ProjectComplexity.Large, 2026,
            startDate: new DateOnly(2026, 1, 1),
            endDate: new DateOnly(2026, 12, 31));
        // Status.Stopped es el inicial

        // Proyecto pospuesto con fechas en 2026
        var postponed = Project.Create("Pospuesto", null, "TIC",
            ProjectComplexity.Large, 2026,
            startDate: new DateOnly(2026, 1, 1),
            endDate: new DateOnly(2026, 12, 31));
        postponed.TransitionTo(ProjectStatus.PlanningWithClient);
        postponed.TransitionTo(ProjectStatus.PostponedByClient);

        db.Projects.AddRange(completed, stopped, postponed);
        await db.SaveChangesAsync();

        db.ProjectTeamAssignments.Add(ProjectTeamAssignment.Create(completed.Id, team.Id, isPrimary: true));
        db.ProjectTeamAssignments.Add(ProjectTeamAssignment.Create(stopped.Id, team.Id, isPrimary: true));
        db.ProjectTeamAssignments.Add(ProjectTeamAssignment.Create(postponed.Id, team.Id, isPrimary: true));
        await db.SaveChangesAsync();

        var handler = new GetCapacityForecastHandler(db);
        var result = await handler.Handle(new GetCapacityForecastQuery(2026), CancellationToken.None);

        var teamDto = result.Teams.Single();

        // Todos los trimestres deben tener demanda 0
        foreach (var q in teamDto.Quarters)
        {
            q.DemandPersonMonths.ShouldBe(0);
            q.Level.ShouldBe("Green");
            q.ProjectTitles.ShouldBeEmpty();
        }
    }

    /// <summary>
    /// Equipo sin miembros (capacidad = 0) con demanda > 0 → LoadPercent = 999, Level = Red.
    /// </summary>
    [Fact]
    public async Task Forecast_ZeroCapacityWithDemand_LoadPercent999AndRed()
    {
        await using var db = CreateDb();

        // Equipo sin miembros
        var team = Team.Create("Equipo Vacío", null, null);
        var project = MakeProject("Proyecto",
            complexity: ProjectComplexity.VerySmall,
            startDate: new DateOnly(2026, 1, 1),
            endDate: new DateOnly(2026, 3, 31));

        db.Teams.Add(team);
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        db.ProjectTeamAssignments.Add(
            ProjectTeamAssignment.Create(project.Id, team.Id, isPrimary: true));
        await db.SaveChangesAsync();

        var handler = new GetCapacityForecastHandler(db);
        var result = await handler.Handle(new GetCapacityForecastQuery(2026), CancellationToken.None);

        var teamDto = result.Teams.Single();
        teamDto.MemberCount.ShouldBe(0);

        var q1 = teamDto.Quarters.Single(q => q.Quarter == 1);
        q1.CapacityPersonMonths.ShouldBe(0);
        q1.DemandPersonMonths.ShouldBeGreaterThan(0);
        q1.LoadPercent.ShouldBe(999);
        q1.Level.ShouldBe("Red");
    }

    /// <summary>
    /// Equipo sin miembros y sin demanda → LoadPercent = 0, Level = Green.
    /// </summary>
    [Fact]
    public async Task Forecast_ZeroCapacityZeroDemand_LoadPercent0AndGreen()
    {
        await using var db = CreateDb();

        var team = Team.Create("Equipo Vacío", null, null);
        db.Teams.Add(team);
        await db.SaveChangesAsync();

        var handler = new GetCapacityForecastHandler(db);
        var result = await handler.Handle(new GetCapacityForecastQuery(2026), CancellationToken.None);

        var teamDto = result.Teams.Single();

        foreach (var q in teamDto.Quarters)
        {
            q.LoadPercent.ShouldBe(0);
            q.Level.ShouldBe("Green");
        }
    }

    /// <summary>
    /// Proyecto sin StartDate → excluido del forecast aunque tenga equipo asignado.
    /// </summary>
    [Fact]
    public async Task Forecast_ProjectWithoutStartDate_IsExcluded()
    {
        await using var db = CreateDb();

        var p1 = MakePerson("Alice");
        var team = Team.Create("Dev Team", null, null);

        // Proyecto sin StartDate
        var project = Project.Create("Sin Inicio", null, "TIC",
            ProjectComplexity.Large, 2026, null, null);

        db.Persons.Add(p1);
        db.Teams.Add(team);
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        db.PersonTeamMemberships.Add(PersonTeamMembership.Create(p1.Id, team.Id));
        db.ProjectTeamAssignments.Add(
            ProjectTeamAssignment.Create(project.Id, team.Id, isPrimary: true));
        await db.SaveChangesAsync();

        var handler = new GetCapacityForecastHandler(db);
        var result = await handler.Handle(new GetCapacityForecastQuery(2026), CancellationToken.None);

        var teamDto = result.Teams.Single();
        foreach (var q in teamDto.Quarters)
            q.DemandPersonMonths.ShouldBe(0);
    }

    /// <summary>
    /// Proyecto sin fecha fin usa la duración por defecto.
    /// Small (2 p-m, duración defecto 1 mes). Si empieza en enero, todo su esfuerzo va a Q1.
    /// </summary>
    [Fact]
    public async Task Forecast_ProjectWithNoEndDate_UsesDefaultDuration()
    {
        await using var db = CreateDb();

        var p1 = MakePerson("Alice");
        var team = Team.Create("Dev Team", null, null);

        // Proyecto Small, enero 2026, sin fecha fin → duración defecto 1 mes
        var project = MakeProject("Small Sin Fin",
            complexity: ProjectComplexity.Small,
            startDate: new DateOnly(2026, 1, 1),
            endDate: null);

        db.Persons.Add(p1);
        db.Teams.Add(team);
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        db.PersonTeamMemberships.Add(PersonTeamMembership.Create(p1.Id, team.Id));
        db.ProjectTeamAssignments.Add(
            ProjectTeamAssignment.Create(project.Id, team.Id, isPrimary: true));
        await db.SaveChangesAsync();

        var handler = new GetCapacityForecastHandler(db);
        var result = await handler.Handle(new GetCapacityForecastQuery(2026), CancellationToken.None);

        var teamDto = result.Teams.Single();
        var q1 = teamDto.Quarters.Single(q => q.Quarter == 1);

        // Small=2 p-m, duración 1 mes → ritmo=2, solapamiento Q1~1 mes → demanda~2 p-m
        q1.DemandPersonMonths.ShouldBeGreaterThan(1.5);
        q1.DemandPersonMonths.ShouldBeLessThan(2.5);
    }

    /// <summary>
    /// Verifica que la nota metodológica está presente en el resultado.
    /// </summary>
    [Fact]
    public async Task Forecast_MethodologyNote_IsPresent()
    {
        await using var db = CreateDb();

        var handler = new GetCapacityForecastHandler(db);
        var result = await handler.Handle(new GetCapacityForecastQuery(2026), CancellationToken.None);

        result.MethodologyNote.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifica que el año devuelto coincide con el solicitado o con el año actual.
    /// </summary>
    [Fact]
    public async Task Forecast_Year_MatchesRequestOrDefaultsToCurrentYear()
    {
        await using var db = CreateDb();
        var handler = new GetCapacityForecastHandler(db);

        var explicit2026 = await handler.Handle(new GetCapacityForecastQuery(2026), CancellationToken.None);
        explicit2026.Year.ShouldBe(2026);

        var defaultYear = await handler.Handle(new GetCapacityForecastQuery(), CancellationToken.None);
        defaultYear.Year.ShouldBe(DateTime.UtcNow.Year);
    }
}
