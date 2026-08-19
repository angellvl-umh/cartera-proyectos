using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Features.Agent;
using CarteraProyectos.Core.Features.Projects;
using CarteraProyectos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace CarteraProyectos.UnitTests.Features.Agent;

public class AgentGovernanceHandlerTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static async Task<Person> AddPersonAsync(AppDbContext db, string email, PersonRole role)
    {
        var person = Person.CreateFromClaims(Guid.NewGuid().ToString(), email.Split('@')[0], email, role);
        db.Persons.Add(person);
        await db.SaveChangesAsync();
        return person;
    }

    private static async Task<Project> AddProjectAsync(AppDbContext db, string title)
    {
        var project = Project.Create(title, null, "TIC", ProjectComplexity.Small, null, null, null);
        db.Projects.Add(project);
        await db.SaveChangesAsync();
        return project;
    }

    private static async Task<Team> AddTeamAsync(AppDbContext db, string name)
    {
        var team = Team.Create(name, null, null);
        db.Teams.Add(team);
        await db.SaveChangesAsync();
        return team;
    }

    private static async Task AssignPersonToTeamAsync(AppDbContext db, int personId, int teamId)
    {
        db.PersonTeamMemberships.Add(PersonTeamMembership.Create(personId, teamId));
        await db.SaveChangesAsync();
    }

    private static async Task AssignProjectToTeamAsync(AppDbContext db, int projectId, int teamId)
    {
        db.ProjectTeamAssignments.Add(ProjectTeamAssignment.Create(projectId, teamId, isPrimary: true));
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task AgentTransitionProjectStatus_MiembroEquipo_CambiaEstado()
    {
        await using var db = CreateDb();
        var dev = await AddPersonAsync(db, "dev@uni.es", PersonRole.Desarrollador);
        var team = await AddTeamAsync(db, "Equipo Alpha");
        await AssignPersonToTeamAsync(db, dev.Id, team.Id);
        var project = await AddProjectAsync(db, "Proyecto 1");
        await AssignProjectToTeamAsync(db, project.Id, team.Id);

        var handler = new AgentTransitionProjectStatusHandler(new ProjectLifecycleService(db));
        await handler.Handle(
            new AgentTransitionProjectStatusCommand(dev.Id, project.Id, "PlanningWithClient"),
            CancellationToken.None);

        var updated = await db.Projects.FindAsync(project.Id);
        updated!.Status.ShouldBe(ProjectStatus.PlanningWithClient);
    }

    [Fact]
    public async Task AgentTransitionProjectStatus_EstadoInvalido_LanzaInvalidOperation()
    {
        await using var db = CreateDb();
        var gestor = await AddPersonAsync(db, "gestor@uni.es", PersonRole.Gestor);
        var project = await AddProjectAsync(db, "Proyecto 1");

        var handler = new AgentTransitionProjectStatusHandler(new ProjectLifecycleService(db));

        await Should.ThrowAsync<InvalidOperationException>(() =>
            handler.Handle(
                new AgentTransitionProjectStatusCommand(gestor.Id, project.Id, "InvalidStatus"),
                CancellationToken.None));
    }

    [Fact]
    public async Task AgentAddProjectRisk_MiembroEquipo_CreaProbabilityBajoImpacto()
    {
        await using var db = CreateDb();
        var dev = await AddPersonAsync(db, "dev@uni.es", PersonRole.Desarrollador);
        var team = await AddTeamAsync(db, "Equipo Alpha");
        await AssignPersonToTeamAsync(db, dev.Id, team.Id);
        var project = await AddProjectAsync(db, "Proyecto 1");
        await AssignProjectToTeamAsync(db, project.Id, team.Id);

        var svc = new ProjectGovernanceService(db);
        var handler = new AgentAddProjectRiskHandler(svc);
        var riskId = await handler.Handle(
            new AgentAddProjectRiskCommand(
                dev.Id, project.Id, "Riesgo de recursos",
                "Medium", "High", "Aumentar equipo"),
            CancellationToken.None);

        var risk = await db.ProjectRisks.FindAsync(riskId);
        risk!.Description.ShouldBe("Riesgo de recursos");
        risk.Probability.ShouldBe(RiskLevel.Medium);
        risk.Impact.ShouldBe(RiskLevel.High);
        risk.Status.ShouldBe(RiskStatus.Open);
    }

    [Fact]
    public async Task AgentAddProjectRisk_InvalidProbability_LanzaInvalidOperation()
    {
        await using var db = CreateDb();
        var gestor = await AddPersonAsync(db, "gestor@uni.es", PersonRole.Gestor);
        var project = await AddProjectAsync(db, "Proyecto 1");

        var svc = new ProjectGovernanceService(db);
        var handler = new AgentAddProjectRiskHandler(svc);

        await Should.ThrowAsync<InvalidOperationException>(() =>
            handler.Handle(
                new AgentAddProjectRiskCommand(
                    gestor.Id, project.Id, "Riesgo",
                    "InvalidLevel", "High", null),
                CancellationToken.None));
    }

    [Fact]
    public async Task AgentUpdateProjectRisk_ActualizaDescripcionYEstado()
    {
        await using var db = CreateDb();
        var gestor = await AddPersonAsync(db, "gestor@uni.es", PersonRole.Gestor);
        var project = await AddProjectAsync(db, "Proyecto 1");
        var risk = ProjectRisk.Create(project.Id, "Riesgo inicial", RiskLevel.Low, RiskLevel.Low, null, gestor.Id);
        db.ProjectRisks.Add(risk);
        await db.SaveChangesAsync();

        var svc = new ProjectGovernanceService(db);
        var handler = new AgentUpdateProjectRiskHandler(svc);
        await handler.Handle(
            new AgentUpdateProjectRiskCommand(
                gestor.Id, project.Id, risk.Id,
                "Riesgo actualizado", "High", "High", "Plan de mitigación", "Mitigated"),
            CancellationToken.None);

        var updated = await db.ProjectRisks.FindAsync(risk.Id);
        updated!.Description.ShouldBe("Riesgo actualizado");
        updated.Probability.ShouldBe(RiskLevel.High);
        updated.Status.ShouldBe(RiskStatus.Mitigated);
    }

    [Fact]
    public async Task AgentAddProjectDependency_CreaDecidenciay()
    {
        await using var db = CreateDb();
        var gestor = await AddPersonAsync(db, "gestor@uni.es", PersonRole.Gestor);
        var proj1 = await AddProjectAsync(db, "Proyecto 1");
        var proj2 = await AddProjectAsync(db, "Proyecto 2");

        var svc = new ProjectGovernanceService(db);
        var handler = new AgentAddProjectDependencyHandler(svc);
        var depId = await handler.Handle(
            new AgentAddProjectDependencyCommand(gestor.Id, proj1.Id, proj2.Id, "Depende de Proyecto 2"),
            CancellationToken.None);

        var dep = await db.ProjectDependencies.FindAsync(depId);
        dep!.ProjectId.ShouldBe(proj1.Id);
        dep.DependsOnProjectId.ShouldBe(proj2.Id);
    }

    [Fact]
    public async Task AgentAddProjectDependency_CycloDirecto_LanzaInvalidOperation()
    {
        await using var db = CreateDb();
        var gestor = await AddPersonAsync(db, "gestor@uni.es", PersonRole.Gestor);
        var proj1 = await AddProjectAsync(db, "Proyecto 1");
        var proj2 = await AddProjectAsync(db, "Proyecto 2");

        // Crear dependencia: proj2 → proj1
        var dep1 = ProjectDependency.Create(proj2.Id, proj1.Id, null);
        db.ProjectDependencies.Add(dep1);
        await db.SaveChangesAsync();

        // Intentar crear ciclo: proj1 → proj2 (cuando proj2 ya depende de proj1)
        var svc = new ProjectGovernanceService(db);
        var handler = new AgentAddProjectDependencyHandler(svc);

        await Should.ThrowAsync<InvalidOperationException>(() =>
            handler.Handle(
                new AgentAddProjectDependencyCommand(gestor.Id, proj1.Id, proj2.Id, null),
                CancellationToken.None));
    }

    [Fact]
    public async Task AgentGetProjectRisks_RetornaTodos()
    {
        await using var db = CreateDb();
        var gestor = await AddPersonAsync(db, "gestor@uni.es", PersonRole.Gestor);
        var project = await AddProjectAsync(db, "Proyecto 1");

        var risk1 = ProjectRisk.Create(project.Id, "Riesgo 1", RiskLevel.Low, RiskLevel.Low, null, gestor.Id);
        var risk2 = ProjectRisk.Create(project.Id, "Riesgo 2", RiskLevel.High, RiskLevel.High, null, gestor.Id);
        db.ProjectRisks.Add(risk1);
        db.ProjectRisks.Add(risk2);
        await db.SaveChangesAsync();

        var svc = new ProjectGovernanceService(db);
        var handler = new AgentGetProjectRisksHandler(svc);
        var risks = await handler.Handle(new AgentGetProjectRisksQuery(project.Id), CancellationToken.None);

        risks.Count.ShouldBe(2);
    }

    [Fact]
    public async Task AgentGetProjectDependencies_RetornaDependenciasYDependents()
    {
        await using var db = CreateDb();
        var gestor = await AddPersonAsync(db, "gestor@uni.es", PersonRole.Gestor);
        var proj1 = await AddProjectAsync(db, "Proyecto 1");
        var proj2 = await AddProjectAsync(db, "Proyecto 2");
        var proj3 = await AddProjectAsync(db, "Proyecto 3");

        // proj1 depende de proj2
        var dep1 = ProjectDependency.Create(proj1.Id, proj2.Id, null);
        // proj3 depende de proj1
        var dep2 = ProjectDependency.Create(proj3.Id, proj1.Id, null);
        db.ProjectDependencies.Add(dep1);
        db.ProjectDependencies.Add(dep2);
        await db.SaveChangesAsync();

        var svc = new ProjectGovernanceService(db);
        var handler = new AgentGetProjectDependenciesHandler(svc);
        var result = await handler.Handle(new AgentGetProjectDependenciesQuery(proj1.Id), CancellationToken.None);

        result.DependsOn.Count.ShouldBe(1); // proj1 depende de proj2
        result.Dependents.Count.ShouldBe(1); // proj3 depende de proj1
    }
}
