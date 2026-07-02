using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Features.Reports;
using CarteraProyectos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace CarteraProyectos.UnitTests.Features.Reports;

public class GetWeeklyPortfolioReportHandlerTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    // ─── Seed helpers ────────────────────────────────────────────────────────

    private static Person MakePerson(string name = "Alice")
        => Person.CreateFromClaims(Guid.NewGuid().ToString(), name, $"{name.ToLower()}@test.com", PersonRole.Desarrollador);

    private static Project MakeProject(string title = "Proyecto Test", ProjectStatus status = ProjectStatus.InSprint, int? year = 2026, SiptGroup? siptGroup = null)
    {
        var p = Project.Create(title, null, "TIC", ProjectComplexity.VerySmall, year, null, null, siptGroup: siptGroup);
        AdvanceProjectTo(p, status);
        return p;
    }

    /// <summary>Avanza el proyecto por rutas válidas hasta el estado deseado (solo para tests).</summary>
    private static void AdvanceProjectTo(Project p, ProjectStatus target)
    {
        if (target == ProjectStatus.Stopped) return;
        // Ruta canónica: Stopped → PlanningWithClient → PlanningSprint → InSprint → InTesting → Completed
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

    private static ProjectWeeklyUpdate MakeUpdate(int projectId, int authorId, ProjectHealthStatus healthStatus, DateOnly? weekOf = null)
    {
        var dateOnly = DateOnly.FromDateTime(DateTime.UtcNow);
        var monday = GetMondayOfWeek(DateTime.UtcNow);
        var weekOfDate = weekOf ?? monday;
        return ProjectWeeklyUpdate.Create(projectId, authorId, weekOfDate, "Test summary", healthStatus);
    }

    private static DateOnly GetMondayOfWeek(DateTime date)
    {
        var dateOnly = DateOnly.FromDateTime(date);
        var daysOfWeek = (int)dateOnly.DayOfWeek;
        var daysToMonday = daysOfWeek == 0 ? 6 : daysOfWeek - 1;
        return dateOnly.AddDays(-daysToMonday);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GetWeeklyPortfolioReport Tests
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ProyectoConUltimaActualizacionAtRisk_EsClasificadoEnRiesgo()
    {
        await using var db = CreateDb();
        var person = MakePerson();
        var project = MakeProject();
        db.Persons.Add(person);
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        var update = MakeUpdate(project.Id, person.Id, ProjectHealthStatus.AtRisk);
        db.ProjectWeeklyUpdates.Add(update);
        await db.SaveChangesAsync();

        var handler = new GetWeeklyPortfolioReportHandler(db);
        var result = await handler.Handle(new GetWeeklyPortfolioReportQuery(), CancellationToken.None);

        result.AtRiskProjects.Count.ShouldBe(1);
        result.AtRiskProjects[0].IsAtRisk.ShouldBeTrue();
        result.AtRiskProjects[0].LatestHealthStatus.ShouldBe("AtRisk");
        result.OtherProjects.ShouldBeEmpty();
    }

    [Fact]
    public async Task ProyectoConUltimaActualizacionBlocked_EsClasificadoEnRiesgo()
    {
        await using var db = CreateDb();
        var person = MakePerson();
        var project = MakeProject();
        db.Persons.Add(person);
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        var update = MakeUpdate(project.Id, person.Id, ProjectHealthStatus.Blocked);
        db.ProjectWeeklyUpdates.Add(update);
        await db.SaveChangesAsync();

        var handler = new GetWeeklyPortfolioReportHandler(db);
        var result = await handler.Handle(new GetWeeklyPortfolioReportQuery(), CancellationToken.None);

        result.AtRiskProjects.Count.ShouldBe(1);
        result.AtRiskProjects[0].IsAtRisk.ShouldBeTrue();
        result.OtherProjects.ShouldBeEmpty();
    }

    [Fact]
    public async Task ProyectoSinActualizacionEstaSemanaISO_EsClasificadoEnRiesgo()
    {
        await using var db = CreateDb();
        var person = MakePerson();
        var project = MakeProject();
        db.Persons.Add(person);
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        // Create update from last week (7 days ago) with OnTrack status
        var lastWeekMonday = GetMondayOfWeek(DateTime.UtcNow).AddDays(-7);
        var oldUpdate = ProjectWeeklyUpdate.Create(project.Id, person.Id, lastWeekMonday, "Old summary", ProjectHealthStatus.OnTrack);
        db.ProjectWeeklyUpdates.Add(oldUpdate);
        await db.SaveChangesAsync();

        var handler = new GetWeeklyPortfolioReportHandler(db);
        var result = await handler.Handle(new GetWeeklyPortfolioReportQuery(), CancellationToken.None);

        // Should be at risk because no update this week, even though old one was OnTrack
        result.AtRiskProjects.Count.ShouldBe(1);
        result.AtRiskProjects[0].HasUpdateThisWeek.ShouldBeFalse();
    }

    [Fact]
    public async Task ProyectoStopped_EsExcluidoDelInforme()
    {
        await using var db = CreateDb();
        var person = MakePerson();
        var project = MakeProject(status: ProjectStatus.Stopped);
        db.Persons.Add(person);
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        var update = MakeUpdate(project.Id, person.Id, ProjectHealthStatus.OnTrack);
        db.ProjectWeeklyUpdates.Add(update);
        await db.SaveChangesAsync();

        var handler = new GetWeeklyPortfolioReportHandler(db);
        var result = await handler.Handle(new GetWeeklyPortfolioReportQuery(), CancellationToken.None);

        result.AtRiskProjects.ShouldBeEmpty();
        result.OtherProjects.ShouldBeEmpty();
    }

    [Fact]
    public async Task ProyectoCompleted_EsExcluidoDelInforme()
    {
        await using var db = CreateDb();
        var person = MakePerson();
        var project = MakeProject(status: ProjectStatus.Completed);
        db.Persons.Add(person);
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        var update = MakeUpdate(project.Id, person.Id, ProjectHealthStatus.OnTrack);
        db.ProjectWeeklyUpdates.Add(update);
        await db.SaveChangesAsync();

        var handler = new GetWeeklyPortfolioReportHandler(db);
        var result = await handler.Handle(new GetWeeklyPortfolioReportQuery(), CancellationToken.None);

        result.AtRiskProjects.ShouldBeEmpty();
        result.OtherProjects.ShouldBeEmpty();
    }

    [Fact]
    public async Task ProyectoConActualizacionOnTrackEstaSemana_NoEstaEnRiesgo()
    {
        await using var db = CreateDb();
        var person = MakePerson();
        var project = MakeProject();
        db.Persons.Add(person);
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        var update = MakeUpdate(project.Id, person.Id, ProjectHealthStatus.OnTrack);
        db.ProjectWeeklyUpdates.Add(update);
        await db.SaveChangesAsync();

        var handler = new GetWeeklyPortfolioReportHandler(db);
        var result = await handler.Handle(new GetWeeklyPortfolioReportQuery(), CancellationToken.None);

        result.AtRiskProjects.ShouldBeEmpty();
        result.OtherProjects.Count.ShouldBe(1);
        result.OtherProjects[0].IsAtRisk.ShouldBeFalse();
        result.OtherProjects[0].HasUpdateThisWeek.ShouldBeTrue();
    }

    [Fact]
    public async Task FiltroPorTeamId_SoloIncluyeProyectosDeEseEquipo()
    {
        await using var db = CreateDb();
        var person = MakePerson();
        var project1 = MakeProject("Proyecto 1");
        var project2 = MakeProject("Proyecto 2");
        var team1 = Team.Create("Equipo 1", null, null);
        var team2 = Team.Create("Equipo 2", null, null);

        db.Persons.Add(person);
        db.Projects.AddRange(project1, project2);
        db.Teams.AddRange(team1, team2);
        await db.SaveChangesAsync();

        db.ProjectTeamAssignments.Add(ProjectTeamAssignment.Create(project1.Id, team1.Id, true));
        db.ProjectTeamAssignments.Add(ProjectTeamAssignment.Create(project2.Id, team2.Id, true));
        await db.SaveChangesAsync();

        var update1 = MakeUpdate(project1.Id, person.Id, ProjectHealthStatus.OnTrack);
        var update2 = MakeUpdate(project2.Id, person.Id, ProjectHealthStatus.OnTrack);
        db.ProjectWeeklyUpdates.AddRange(update1, update2);
        await db.SaveChangesAsync();

        var handler = new GetWeeklyPortfolioReportHandler(db);
        var result = await handler.Handle(new GetWeeklyPortfolioReportQuery(TeamId: team1.Id), CancellationToken.None);

        result.OtherProjects.Count.ShouldBe(1);
        result.OtherProjects[0].Title.ShouldBe("Proyecto 1");
    }

    [Fact]
    public async Task FiltroPorYear_SoloIncluyeProyectosDeEseAnio()
    {
        await using var db = CreateDb();
        var person = MakePerson();
        var project2025 = MakeProject("Proyecto 2025", year: 2025);
        var project2026 = MakeProject("Proyecto 2026", year: 2026);

        db.Persons.Add(person);
        db.Projects.AddRange(project2025, project2026);
        await db.SaveChangesAsync();

        var update1 = MakeUpdate(project2025.Id, person.Id, ProjectHealthStatus.OnTrack);
        var update2 = MakeUpdate(project2026.Id, person.Id, ProjectHealthStatus.OnTrack);
        db.ProjectWeeklyUpdates.AddRange(update1, update2);
        await db.SaveChangesAsync();

        var handler = new GetWeeklyPortfolioReportHandler(db);
        var result = await handler.Handle(new GetWeeklyPortfolioReportQuery(Year: 2025), CancellationToken.None);

        result.OtherProjects.Count.ShouldBe(1);
        result.OtherProjects[0].PortfolioYear.ShouldBe(2025);
    }

    [Fact]
    public async Task ProyectoSinNingunaActualizacion_EsClasificadoEnRiesgo()
    {
        await using var db = CreateDb();
        var project = MakeProject();
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        var handler = new GetWeeklyPortfolioReportHandler(db);
        var result = await handler.Handle(new GetWeeklyPortfolioReportQuery(), CancellationToken.None);

        result.AtRiskProjects.Count.ShouldBe(1);
        result.AtRiskProjects[0].IsAtRisk.ShouldBeTrue();
        result.AtRiskProjects[0].LatestSummary.ShouldBeNull();
        result.AtRiskProjects[0].HasUpdateThisWeek.ShouldBeFalse();
    }

    [Fact]
    public async Task FiltroPorSiptGroup_SoloIncluyeProyectosDeEseGrupo()
    {
        await using var db = CreateDb();
        var person = MakePerson();
        var projectRRHH = MakeProject("Proyecto RRHH", siptGroup: SiptGroup.RRHH);
        var projectAcademico = MakeProject("Proyecto Academico", siptGroup: SiptGroup.Academico);

        db.Persons.Add(person);
        db.Projects.AddRange(projectRRHH, projectAcademico);
        await db.SaveChangesAsync();

        var update1 = MakeUpdate(projectRRHH.Id, person.Id, ProjectHealthStatus.OnTrack);
        var update2 = MakeUpdate(projectAcademico.Id, person.Id, ProjectHealthStatus.OnTrack);
        db.ProjectWeeklyUpdates.AddRange(update1, update2);
        await db.SaveChangesAsync();

        var handler = new GetWeeklyPortfolioReportHandler(db);
        var result = await handler.Handle(new GetWeeklyPortfolioReportQuery(SiptGroup: "RRHH"), CancellationToken.None);

        result.OtherProjects.Count.ShouldBe(1);
        result.OtherProjects[0].Title.ShouldBe("Proyecto RRHH");
    }

    [Fact]
    public async Task ProyectoConEquipoPrimario_IncluyeNombreDelEquipo()
    {
        await using var db = CreateDb();
        var person = MakePerson();
        var project = MakeProject();
        var team = Team.Create("Equipo Principal", null, null);

        db.Persons.Add(person);
        db.Projects.Add(project);
        db.Teams.Add(team);
        await db.SaveChangesAsync();

        db.ProjectTeamAssignments.Add(ProjectTeamAssignment.Create(project.Id, team.Id, isPrimary: true));
        await db.SaveChangesAsync();

        var update = MakeUpdate(project.Id, person.Id, ProjectHealthStatus.OnTrack);
        db.ProjectWeeklyUpdates.Add(update);
        await db.SaveChangesAsync();

        var handler = new GetWeeklyPortfolioReportHandler(db);
        var result = await handler.Handle(new GetWeeklyPortfolioReportQuery(), CancellationToken.None);

        result.OtherProjects[0].PrimaryTeamName.ShouldBe("Equipo Principal");
    }

    [Fact]
    public async Task AtRiskProjectsYOtherProjects_OrdenadasPorTitulo()
    {
        await using var db = CreateDb();
        var person = MakePerson();
        var projectZ = MakeProject("Proyecto Z");
        var projectA = MakeProject("Proyecto A");
        var projectM = MakeProject("Proyecto M");

        db.Persons.Add(person);
        db.Projects.AddRange(projectZ, projectA, projectM);
        await db.SaveChangesAsync();

        var updateA = MakeUpdate(projectA.Id, person.Id, ProjectHealthStatus.AtRisk);
        var updateM = MakeUpdate(projectM.Id, person.Id, ProjectHealthStatus.OnTrack);
        var updateZ = MakeUpdate(projectZ.Id, person.Id, ProjectHealthStatus.OnTrack);
        db.ProjectWeeklyUpdates.AddRange(updateA, updateM, updateZ);
        await db.SaveChangesAsync();

        var handler = new GetWeeklyPortfolioReportHandler(db);
        var result = await handler.Handle(new GetWeeklyPortfolioReportQuery(), CancellationToken.None);

        result.AtRiskProjects[0].Title.ShouldBe("Proyecto A");
        result.OtherProjects[0].Title.ShouldBe("Proyecto M");
        result.OtherProjects[1].Title.ShouldBe("Proyecto Z");
    }

    [Fact]
    public async Task MultipleActualizacionesPorProyecto_UltimaEsSeleccionada()
    {
        await using var db = CreateDb();
        var person = MakePerson();
        var project = MakeProject();
        db.Persons.Add(person);
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        var monday = GetMondayOfWeek(DateTime.UtcNow);
        var oldUpdate = ProjectWeeklyUpdate.Create(project.Id, person.Id, monday.AddDays(-1), "Old", ProjectHealthStatus.OnTrack);
        var newUpdate = ProjectWeeklyUpdate.Create(project.Id, person.Id, monday, "New", ProjectHealthStatus.AtRisk);
        db.ProjectWeeklyUpdates.AddRange(oldUpdate, newUpdate);
        await db.SaveChangesAsync();

        var handler = new GetWeeklyPortfolioReportHandler(db);
        var result = await handler.Handle(new GetWeeklyPortfolioReportQuery(), CancellationToken.None);

        result.AtRiskProjects[0].LatestSummary.ShouldBe("New");
        result.AtRiskProjects[0].LatestHealthStatus.ShouldBe("AtRisk");
    }
}
