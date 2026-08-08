using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Features.Agent;
using CarteraProyectos.Core.Interfaces;
using CarteraProyectos.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;

namespace CarteraProyectos.UnitTests.Features.Agent;

// ── Fakes inline ─────────────────────────────────────────────────────────────

file sealed class FakeBlobStore : IEphemeralBlobStore
{
    public List<(byte[] Data, string ContentType, string? FileName)> Stored { get; } = [];

    public Guid Store(byte[] data, string contentType, string? fileName)
    {
        Stored.Add((data, contentType, fileName));
        return Guid.NewGuid();
    }

    public EphemeralBlob? TryGet(Guid id) => null;
}

file sealed class FakeUrlProvider : IPublicUrlProvider
{
    public string BuildChartUrl(Guid id)  => $"https://fake/charts/{id}";
    public string BuildExportUrl(Guid id) => $"https://fake/exports/{id}";
}

// ── Tests ─────────────────────────────────────────────────────────────────────

public class AgentExportChartHandlerTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static ISender CreateSender(AppDbContext db)
    {
        var services = new ServiceCollection();
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(AgentExportProjectsExcelQuery).Assembly));
        services.AddScoped<IAppDbContext>(sp => db);
        return services.BuildServiceProvider().GetRequiredService<ISender>();
    }

    private static async Task<Person> AddGestorAsync(AppDbContext db)
    {
        var gestor = Person.CreateFromClaims(Guid.NewGuid().ToString(),
            "gestor", "gestor@uni.es", PersonRole.Gestor);
        db.Persons.Add(gestor);
        await db.SaveChangesAsync();
        return gestor;
    }

    private static async Task<Person> AddDevAsync(AppDbContext db, string name = "Dev")
    {
        var dev = Person.CreateFromClaims(Guid.NewGuid().ToString(),
            name, $"{name.ToLower()}@uni.es", PersonRole.Desarrollador);
        db.Persons.Add(dev);
        await db.SaveChangesAsync();
        return dev;
    }

    private static async Task<Project> AddProjectAsync(AppDbContext db, string title = "Proyecto Test")
    {
        var project = Project.Create(title, null, "TIC", ProjectComplexity.Small, null, null, null);
        db.Projects.Add(project);
        await db.SaveChangesAsync();
        return project;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // AgentExportProjectsExcelHandler
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ExportProjects_ListaVacia_UrlNullYMensajeInformativo_NoBlobStore()
    {
        await using var db = CreateDb();
        var gestor = await AddGestorAsync(db);
        // Sin proyectos en BD

        var blob = new FakeBlobStore();
        var url  = new FakeUrlProvider();
        var handler = new AgentExportProjectsExcelHandler(CreateSender(db), blob, url);

        var result = await handler.Handle(
            new AgentExportProjectsExcelQuery(gestor.Id, null),
            CancellationToken.None);

        result.Url.ShouldBeNull();
        result.Message.ShouldNotBeNullOrEmpty();
        blob.Stored.ShouldBeEmpty();
    }

    [Fact]
    public async Task ExportProjects_ConProyectos_UrlCoincideConFakeProvider()
    {
        await using var db = CreateDb();
        var gestor = await AddGestorAsync(db);
        await AddProjectAsync(db, "Proyecto A");

        var blob = new FakeBlobStore();
        var url  = new FakeUrlProvider();
        var handler = new AgentExportProjectsExcelHandler(CreateSender(db), blob, url);

        var result = await handler.Handle(
            new AgentExportProjectsExcelQuery(gestor.Id, null),
            CancellationToken.None);

        result.Url.ShouldNotBeNull();
        result.Url!.ShouldStartWith("https://fake/exports/");
        blob.Stored.Count.ShouldBe(1);
        blob.Stored[0].ContentType.ShouldContain("spreadsheetml");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // AgentExportWeeklyReportExcelHandler
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ExportWeeklyReport_SinProyectosActivos_UrlNull()
    {
        await using var db = CreateDb();
        // Sin proyectos activos — solo proyectos Stopped (inicial) no entran en el report
        var project = Project.Create("Parado", null, "TIC", ProjectComplexity.VerySmall, 2026, null, null);
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        var blob = new FakeBlobStore();
        var url  = new FakeUrlProvider();
        var handler = new AgentExportWeeklyReportExcelHandler(CreateSender(db), blob, url);

        var result = await handler.Handle(
            new AgentExportWeeklyReportExcelQuery(1, null, null),
            CancellationToken.None);

        result.Url.ShouldBeNull();
        blob.Stored.ShouldBeEmpty();
    }

    [Fact]
    public async Task ExportWeeklyReport_ConProyectosActivos_UrlNoNull()
    {
        await using var db = CreateDb();
        // Proyecto activo (no Stopped/Completed/PostponedByClient)
        var project = Project.Create("Activo", null, "TIC", ProjectComplexity.VerySmall, 2026, null, null);
        project.TransitionTo(ProjectStatus.PlanningWithClient);
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        var blob = new FakeBlobStore();
        var url  = new FakeUrlProvider();
        var handler = new AgentExportWeeklyReportExcelHandler(CreateSender(db), blob, url);

        var result = await handler.Handle(
            new AgentExportWeeklyReportExcelQuery(1, null, null),
            CancellationToken.None);

        result.Url.ShouldNotBeNull();
        result.Url!.ShouldStartWith("https://fake/exports/");
        blob.Stored.Count.ShouldBe(1);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // AgentChartMyTasksByStatusHandler
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ChartMyTasksByStatus_SinTareas_UrlNull()
    {
        await using var db = CreateDb();
        var dev = await AddDevAsync(db);

        var blob = new FakeBlobStore();
        var url  = new FakeUrlProvider();
        var handler = new AgentChartMyTasksByStatusHandler(CreateSender(db), blob, url);

        var result = await handler.Handle(
            new AgentChartMyTasksByStatusQuery(dev.Id, "donut"),
            CancellationToken.None);

        result.Url.ShouldBeNull();
        result.Message.ShouldNotBeNullOrEmpty();
        blob.Stored.ShouldBeEmpty();
    }

    [Fact]
    public async Task ChartMyTasksByStatus_ConTareasVariadas_AgregaPorEstadoCorrectamente()
    {
        await using var db = CreateDb();
        var dev = await AddDevAsync(db);
        var project = await AddProjectAsync(db);

        // 1 InProgress, 2 ToDo, 1 Backlog → cuatro asignadas
        var wi1 = WorkItem.Create(project.Id, "T1", null, WorkItemPriority.Medium, null, 0, null, false, null, null);
        wi1.TransitionStatus(WorkItemStatus.InProgress);
        var wi2 = WorkItem.Create(project.Id, "T2", null, WorkItemPriority.Medium, null, 1, null, false, null, null);
        wi2.TransitionStatus(WorkItemStatus.ToDo);
        var wi3 = WorkItem.Create(project.Id, "T3", null, WorkItemPriority.Medium, null, 2, null, false, null, null);
        wi3.TransitionStatus(WorkItemStatus.ToDo);
        var wi4 = WorkItem.Create(project.Id, "T4", null, WorkItemPriority.Medium, null, 3, null, false, null, null);
        // Status = Backlog (por defecto)

        db.WorkItems.AddRange(wi1, wi2, wi3, wi4);
        await db.SaveChangesAsync();

        ((List<Person>)wi1.Assignees).Add(dev);
        ((List<Person>)wi2.Assignees).Add(dev);
        ((List<Person>)wi3.Assignees).Add(dev);
        ((List<Person>)wi4.Assignees).Add(dev);
        await db.SaveChangesAsync();

        var blob = new FakeBlobStore();
        var url  = new FakeUrlProvider();
        var handler = new AgentChartMyTasksByStatusHandler(CreateSender(db), blob, url);

        var result = await handler.Handle(
            new AgentChartMyTasksByStatusQuery(dev.Id, "donut"),
            CancellationToken.None);

        result.Url.ShouldNotBeNull();
        result.Url!.ShouldStartWith("https://fake/charts/");
        blob.Stored.Count.ShouldBe(1);
        blob.Stored[0].ContentType.ShouldBe("image/svg+xml");

        // El SVG debe contener los estados correctos
        var svgContent = System.Text.Encoding.UTF8.GetString(blob.Stored[0].Data);
        svgContent.ShouldContain("InProgress");
        svgContent.ShouldContain("ToDo");
        svgContent.ShouldContain("Backlog");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // AgentChartProjectsByTeamHandler
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ChartProjectsByTeam_SinProyectos_UrlNull()
    {
        await using var db = CreateDb();
        var gestor = await AddGestorAsync(db);

        var blob = new FakeBlobStore();
        var url  = new FakeUrlProvider();
        var handler = new AgentChartProjectsByTeamHandler(CreateSender(db), blob, url);

        var result = await handler.Handle(
            new AgentChartProjectsByTeamQuery(gestor.Id, "bar"),
            CancellationToken.None);

        result.Url.ShouldBeNull();
        blob.Stored.ShouldBeEmpty();
    }

    [Fact]
    public async Task ChartProjectsByTeam_NullPrimaryTeam_ConvertidoASinEquipo()
    {
        await using var db = CreateDb();
        var gestor = await AddGestorAsync(db);

        // Proyecto sin equipo asignado → PrimaryTeamName = null → "Sin equipo"
        await AddProjectAsync(db, "Proyecto Sin Equipo");

        var blob = new FakeBlobStore();
        var url  = new FakeUrlProvider();
        var handler = new AgentChartProjectsByTeamHandler(CreateSender(db), blob, url);

        var result = await handler.Handle(
            new AgentChartProjectsByTeamQuery(gestor.Id, "bar"),
            CancellationToken.None);

        result.Url.ShouldNotBeNull();
        var svgContent = System.Text.Encoding.UTF8.GetString(blob.Stored[0].Data);
        svgContent.ShouldContain("Sin equipo");
    }

    [Fact]
    public async Task ChartProjectsByTeam_ConEquiposAsignados_AgrupaPorEquipoPrimario()
    {
        await using var db = CreateDb();
        var gestor = await AddGestorAsync(db);

        var team = Team.Create("Equipo Alpha", null, null);
        db.Teams.Add(team);
        var p1 = await AddProjectAsync(db, "Proyecto 1");
        var p2 = await AddProjectAsync(db, "Proyecto 2");
        await db.SaveChangesAsync();

        db.ProjectTeamAssignments.Add(ProjectTeamAssignment.Create(p1.Id, team.Id, isPrimary: true));
        db.ProjectTeamAssignments.Add(ProjectTeamAssignment.Create(p2.Id, team.Id, isPrimary: true));
        await db.SaveChangesAsync();

        var blob = new FakeBlobStore();
        var url  = new FakeUrlProvider();
        var handler = new AgentChartProjectsByTeamHandler(CreateSender(db), blob, url);

        var result = await handler.Handle(
            new AgentChartProjectsByTeamQuery(gestor.Id, "bar"),
            CancellationToken.None);

        result.Url.ShouldNotBeNull();
        var svgContent = System.Text.Encoding.UTF8.GetString(blob.Stored[0].Data);
        svgContent.ShouldContain("Equipo Alpha");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // AgentChartTeamCapacityHandler — happy path + sin datos
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ChartTeamCapacity_SinEquipos_UrlNull()
    {
        await using var db = CreateDb();
        var gestor = await AddGestorAsync(db);

        var blob = new FakeBlobStore();
        var url  = new FakeUrlProvider();
        var handler = new AgentChartTeamCapacityHandler(CreateSender(db), blob, url);

        var result = await handler.Handle(
            new AgentChartTeamCapacityQuery(gestor.Id),
            CancellationToken.None);

        result.Url.ShouldBeNull();
        blob.Stored.ShouldBeEmpty();
    }

    [Fact]
    public async Task ChartTeamCapacity_ConMiembroConTareas_UrlNoNull()
    {
        await using var db = CreateDb();
        var dev = await AddDevAsync(db, "Ana");

        var team = Team.Create("Equipo Alpha", null, null);
        db.Teams.Add(team);
        await db.SaveChangesAsync();

        db.PersonTeamMemberships.Add(PersonTeamMembership.Create(dev.Id, team.Id));
        var project = await AddProjectAsync(db);
        await db.SaveChangesAsync();

        var wi = WorkItem.Create(project.Id, "T1", null, WorkItemPriority.Medium, null, 0, null, false, null, null);
        wi.TransitionStatus(WorkItemStatus.InProgress);
        db.WorkItems.Add(wi);
        await db.SaveChangesAsync();

        ((List<Person>)wi.Assignees).Add(dev);
        await db.SaveChangesAsync();

        var blob = new FakeBlobStore();
        var url  = new FakeUrlProvider();
        var handler = new AgentChartTeamCapacityHandler(CreateSender(db), blob, url);

        var result = await handler.Handle(
            new AgentChartTeamCapacityQuery(dev.Id),
            CancellationToken.None);

        result.Url.ShouldNotBeNull();
        blob.Stored.Count.ShouldBe(1);
        blob.Stored[0].ContentType.ShouldBe("image/svg+xml");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // AgentChartProjectProgressHandler — happy path + sin datos
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ChartProjectProgress_SinProyectos_UrlNull()
    {
        await using var db = CreateDb();
        var gestor = await AddGestorAsync(db);

        var blob = new FakeBlobStore();
        var url  = new FakeUrlProvider();
        var handler = new AgentChartProjectProgressHandler(CreateSender(db), blob, url);

        var result = await handler.Handle(
            new AgentChartProjectProgressQuery(gestor.Id),
            CancellationToken.None);

        result.Url.ShouldBeNull();
        blob.Stored.ShouldBeEmpty();
    }

    [Fact]
    public async Task ChartProjectProgress_ConProyectos_GeneraUrl()
    {
        await using var db = CreateDb();
        var gestor = await AddGestorAsync(db);
        await AddProjectAsync(db, "Proyecto Alpha");

        var blob = new FakeBlobStore();
        var url  = new FakeUrlProvider();
        var handler = new AgentChartProjectProgressHandler(CreateSender(db), blob, url);

        var result = await handler.Handle(
            new AgentChartProjectProgressQuery(gestor.Id),
            CancellationToken.None);

        result.Url.ShouldNotBeNull();
        result.Url!.ShouldStartWith("https://fake/charts/");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // AgentChartProjectsByStatusHandler — happy path + sin datos
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ChartProjectsByStatus_SinProyectos_UrlNull()
    {
        await using var db = CreateDb();
        var gestor = await AddGestorAsync(db);

        var blob = new FakeBlobStore();
        var url  = new FakeUrlProvider();
        var handler = new AgentChartProjectsByStatusHandler(CreateSender(db), blob, url);

        var result = await handler.Handle(
            new AgentChartProjectsByStatusQuery(gestor.Id, null, "pie"),
            CancellationToken.None);

        result.Url.ShouldBeNull();
        blob.Stored.ShouldBeEmpty();
    }

    [Fact]
    public async Task ChartProjectsByStatus_ConProyectos_GeneraUrl()
    {
        await using var db = CreateDb();
        var gestor = await AddGestorAsync(db);
        await AddProjectAsync(db, "Proyecto X");

        var blob = new FakeBlobStore();
        var url  = new FakeUrlProvider();
        var handler = new AgentChartProjectsByStatusHandler(CreateSender(db), blob, url);

        var result = await handler.Handle(
            new AgentChartProjectsByStatusQuery(gestor.Id, null, "pie"),
            CancellationToken.None);

        result.Url.ShouldNotBeNull();
        blob.Stored.Count.ShouldBe(1);
    }
}
