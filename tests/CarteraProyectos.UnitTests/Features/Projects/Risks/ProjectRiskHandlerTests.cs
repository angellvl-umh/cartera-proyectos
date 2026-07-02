using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Features.Projects.Risks;
using CarteraProyectos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace CarteraProyectos.UnitTests.Features.Projects.Risks;

public class ProjectRiskHandlerTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static async Task<(Project project, Person gestor, int projectId)> SeedProjectWithGestorAsync(AppDbContext db)
    {
        var gestor = Person.CreateFromClaims("sub-gestor", "Gestor Test", "gestor@test.com", PersonRole.Gestor);
        var project = Project.Create("Proyecto Test", null, "TIC", ProjectComplexity.Small, 2026, null, null);
        db.Persons.Add(gestor);
        db.Projects.Add(project);
        await db.SaveChangesAsync();
        return (project, gestor, project.Id);
    }

    private static async Task<(Project project, Person jefe, Team team, int projectId)> SeedProjectWithJefeAsync(AppDbContext db)
    {
        var jefe = Person.CreateFromClaims("sub-jefe", "Jefe Test", "jefe@test.com", PersonRole.JefeEquipo);
        var project = Project.Create("Proyecto Test JE", null, "TIC", ProjectComplexity.Small, 2026, null, null);
        var team = Team.Create("Equipo Test", null, null);

        db.Persons.Add(jefe);
        db.Projects.Add(project);
        db.Teams.Add(team);
        await db.SaveChangesAsync();

        // Asignar jefe como lead del equipo
        typeof(Team).GetProperty("LeadPersonId")!.SetValue(team, jefe.Id);
        await db.SaveChangesAsync();

        db.ProjectTeamAssignments.Add(ProjectTeamAssignment.Create(project.Id, team.Id, true));
        await db.SaveChangesAsync();

        return (project, jefe, team, project.Id);
    }

    // ─── Severity ────────────────────────────────────────────────────────────

    [Fact]
    public void Severity_HighHigh_Returns9()
    {
        var risk = ProjectRisk.Create(1, "desc", RiskLevel.High, RiskLevel.High, null, 1);
        risk.Severity.ShouldBe(9); // (2+1)*(2+1) = 9
    }

    [Fact]
    public void Severity_LowLow_Returns1()
    {
        var risk = ProjectRisk.Create(1, "desc", RiskLevel.Low, RiskLevel.Low, null, 1);
        risk.Severity.ShouldBe(1); // (0+1)*(0+1) = 1
    }

    [Fact]
    public void Severity_MediumHigh_Returns6()
    {
        var risk = ProjectRisk.Create(1, "desc", RiskLevel.Medium, RiskLevel.High, null, 1);
        risk.Severity.ShouldBe(6); // (1+1)*(2+1) = 6
    }

    [Fact]
    public void Severity_HighLow_Returns3()
    {
        var risk = ProjectRisk.Create(1, "desc", RiskLevel.High, RiskLevel.Low, null, 1);
        risk.Severity.ShouldBe(3); // (2+1)*(0+1) = 3
    }

    // ─── CreateProjectRisk ────────────────────────────────────────────────────

    [Fact]
    public async Task CreateRisk_Gestor_CreatesSuccessfully()
    {
        await using var db = CreateDb();
        var (project, gestor, projectId) = await SeedProjectWithGestorAsync(db);

        var handler = new CreateProjectRiskHandler(db);
        var id = await handler.Handle(
            new CreateProjectRiskCommand(projectId, gestor.Id, "Riesgo de integración",
                RiskLevel.High, RiskLevel.Medium, "Mitigación: pruebas tempranas"),
            CancellationToken.None);

        id.ShouldBeGreaterThan(0);
        var risk = await db.ProjectRisks.FindAsync(id);
        risk.ShouldNotBeNull();
        risk.Description.ShouldBe("Riesgo de integración");
        risk.Status.ShouldBe(RiskStatus.Open);
        risk.Probability.ShouldBe(RiskLevel.High);
        risk.Impact.ShouldBe(RiskLevel.Medium);
    }

    [Fact]
    public async Task CreateRisk_DevMiembroEquipoProyecto_CreatesSuccessfully()
    {
        await using var db = CreateDb();
        var dev = Person.CreateFromClaims("sub-dev", "Dev", "dev@test.com", PersonRole.Desarrollador);
        var project = Project.Create("P", null, null, ProjectComplexity.VerySmall, null, null, null);
        var team = Team.Create("Equipo", null, null);
        db.Persons.Add(dev);
        db.Projects.Add(project);
        db.Teams.Add(team);
        await db.SaveChangesAsync();

        db.PersonTeamMemberships.Add(PersonTeamMembership.Create(dev.Id, team.Id));
        db.ProjectTeamAssignments.Add(ProjectTeamAssignment.Create(project.Id, team.Id, true));
        await db.SaveChangesAsync();

        var handler = new CreateProjectRiskHandler(db);
        var id = await handler.Handle(
            new CreateProjectRiskCommand(project.Id, dev.Id, "Riesgo", RiskLevel.Low, RiskLevel.Low, null),
            CancellationToken.None);

        id.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task CreateRisk_PersonaAjenaAlProyecto_ThrowsUnauthorized()
    {
        await using var db = CreateDb();
        var outsider = Person.CreateFromClaims("sub-out", "Outsider", "out@test.com", PersonRole.Desarrollador);
        var project = Project.Create("P", null, null, ProjectComplexity.VerySmall, null, null, null);
        db.Persons.Add(outsider);
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        var handler = new CreateProjectRiskHandler(db);
        await Should.ThrowAsync<UnauthorizedAccessException>(
            () => handler.Handle(
                new CreateProjectRiskCommand(project.Id, outsider.Id, "Riesgo", RiskLevel.Low, RiskLevel.Low, null),
                CancellationToken.None));
    }

    [Fact]
    public async Task CreateRisk_ProjectNotFound_ThrowsKeyNotFound()
    {
        await using var db = CreateDb();
        var gestor = Person.CreateFromClaims("sub-g", "Gestor", "g@test.com", PersonRole.Gestor);
        db.Persons.Add(gestor);
        await db.SaveChangesAsync();

        var handler = new CreateProjectRiskHandler(db);
        await Should.ThrowAsync<KeyNotFoundException>(
            () => handler.Handle(
                new CreateProjectRiskCommand(999, gestor.Id, "Riesgo", RiskLevel.Low, RiskLevel.Low, null),
                CancellationToken.None));
    }

    // ─── UpdateProjectRisk ────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateRisk_Gestor_UpdatesSuccessfully()
    {
        await using var db = CreateDb();
        var (project, gestor, projectId) = await SeedProjectWithGestorAsync(db);
        var risk = ProjectRisk.Create(projectId, "Original", RiskLevel.Low, RiskLevel.Low, null, gestor.Id);
        db.ProjectRisks.Add(risk);
        await db.SaveChangesAsync();

        var handler = new UpdateProjectRiskHandler(db);
        await handler.Handle(
            new UpdateProjectRiskCommand(projectId, risk.Id, gestor.Id,
                "Actualizado", RiskLevel.High, RiskLevel.High, "Nueva mitigación", RiskStatus.Mitigated),
            CancellationToken.None);

        var updated = await db.ProjectRisks.FindAsync(risk.Id);
        updated!.Description.ShouldBe("Actualizado");
        updated.Status.ShouldBe(RiskStatus.Mitigated);
        updated.Probability.ShouldBe(RiskLevel.High);
    }

    [Fact]
    public async Task UpdateRisk_NotFound_ThrowsKeyNotFound()
    {
        await using var db = CreateDb();
        var (project, gestor, projectId) = await SeedProjectWithGestorAsync(db);

        var handler = new UpdateProjectRiskHandler(db);
        await Should.ThrowAsync<KeyNotFoundException>(
            () => handler.Handle(
                new UpdateProjectRiskCommand(projectId, 999, gestor.Id,
                    "X", RiskLevel.Low, RiskLevel.Low, null, RiskStatus.Open),
                CancellationToken.None));
    }

    // ─── DeleteProjectRisk ────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteRisk_Gestor_DeletesSuccessfully()
    {
        await using var db = CreateDb();
        var (project, gestor, projectId) = await SeedProjectWithGestorAsync(db);
        var risk = ProjectRisk.Create(projectId, "A borrar", RiskLevel.Low, RiskLevel.Low, null, gestor.Id);
        db.ProjectRisks.Add(risk);
        await db.SaveChangesAsync();

        var handler = new DeleteProjectRiskHandler(db);
        await handler.Handle(new DeleteProjectRiskCommand(projectId, risk.Id, gestor.Id), CancellationToken.None);

        (await db.ProjectRisks.FindAsync(risk.Id)).ShouldBeNull();
    }

    [Fact]
    public async Task DeleteRisk_NotFound_ThrowsKeyNotFound()
    {
        await using var db = CreateDb();
        var (project, gestor, projectId) = await SeedProjectWithGestorAsync(db);

        var handler = new DeleteProjectRiskHandler(db);
        await Should.ThrowAsync<KeyNotFoundException>(
            () => handler.Handle(
                new DeleteProjectRiskCommand(projectId, 999, gestor.Id),
                CancellationToken.None));
    }

    // ─── GetProjectRisks (orden) ──────────────────────────────────────────────

    [Fact]
    public async Task GetRisks_OrderedByStatusThenSeverityDesc()
    {
        await using var db = CreateDb();
        var (project, gestor, projectId) = await SeedProjectWithGestorAsync(db);

        // Crear riesgos con distintos estados y severidades
        var r1 = ProjectRisk.Create(projectId, "Closed-HighHigh", RiskLevel.High, RiskLevel.High, null, gestor.Id);
        var r2 = ProjectRisk.Create(projectId, "Open-LowLow", RiskLevel.Low, RiskLevel.Low, null, gestor.Id);
        var r3 = ProjectRisk.Create(projectId, "Open-HighHigh", RiskLevel.High, RiskLevel.High, null, gestor.Id);
        var r4 = ProjectRisk.Create(projectId, "Mitigated-Med", RiskLevel.Medium, RiskLevel.Medium, null, gestor.Id);

        // r1 → Closed, r4 → Mitigated
        r1.Update("Closed-HighHigh", RiskLevel.High, RiskLevel.High, null, RiskStatus.Closed);
        r4.Update("Mitigated-Med", RiskLevel.Medium, RiskLevel.Medium, null, RiskStatus.Mitigated);

        db.ProjectRisks.AddRange(r1, r2, r3, r4);
        await db.SaveChangesAsync();

        var handler = new GetProjectRisksHandler(db);
        var result = await handler.Handle(new GetProjectRisksQuery(projectId), CancellationToken.None);

        result.Total.ShouldBe(4);
        // Orden esperado: Open-HighHigh (severity 9), Open-LowLow (severity 1), Mitigated-Med, Closed-HighHigh
        result.Items[0].Description.ShouldBe("Open-HighHigh");
        result.Items[1].Description.ShouldBe("Open-LowLow");
        result.Items[2].Description.ShouldBe("Mitigated-Med");
        result.Items[3].Description.ShouldBe("Closed-HighHigh");
    }

    [Fact]
    public async Task GetRisks_SeverityInDto_IsCorrect()
    {
        await using var db = CreateDb();
        var (project, gestor, projectId) = await SeedProjectWithGestorAsync(db);

        var risk = ProjectRisk.Create(projectId, "Test", RiskLevel.High, RiskLevel.High, null, gestor.Id);
        db.ProjectRisks.Add(risk);
        await db.SaveChangesAsync();

        var handler = new GetProjectRisksHandler(db);
        var result = await handler.Handle(new GetProjectRisksQuery(projectId), CancellationToken.None);

        result.Items[0].Severity.ShouldBe(9);
    }
}
