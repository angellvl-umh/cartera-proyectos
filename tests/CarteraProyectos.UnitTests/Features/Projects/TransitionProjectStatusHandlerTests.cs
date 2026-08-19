using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Features.Projects;
using CarteraProyectos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace CarteraProyectos.UnitTests.Features.Projects;

public class TransitionProjectStatusHandlerTests
{
    private static AppDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static async Task<(AppDbContext db, Project project, Person gestor)> SetupProjectWithGestor()
    {
        var db = CreateInMemoryContext();

        var gestor = Person.CreateFromClaims("sub-gestor", "Gestor User", "gestor@test.com", PersonRole.Gestor);
        db.Persons.Add(gestor);

        var project = Project.Create("Proyecto Test", null, "Unidad TIC", ProjectComplexity.Medium, null, null, null);
        db.Projects.Add(project);

        await db.SaveChangesAsync();
        return (db, project, gestor);
    }

    [Fact]
    public async Task Handle_GestorCambiaAnyStatus_Succeeds()
    {
        var (db, project, gestor) = await SetupProjectWithGestor();
        await using var _ = db;

        var handler = new TransitionProjectStatusHandler(new ProjectLifecycleService(db));
        // Stopped → PlanningWithClient es una transición válida
        var cmd = new TransitionProjectStatusCommand(project.Id, ProjectStatus.PlanningWithClient, gestor.Id);

        await handler.Handle(cmd, CancellationToken.None);

        var updated = await db.Projects.FindAsync(project.Id);
        updated!.Status.ShouldBe(ProjectStatus.PlanningWithClient);
    }

    [Fact]
    public async Task Handle_GestorCambiaACompleted_Succeeds()
    {
        var (db, project, gestor) = await SetupProjectWithGestor();
        await using var _ = db;

        var handler = new TransitionProjectStatusHandler(new ProjectLifecycleService(db));

        // Avanzar por ruta válida hasta InTesting: Stopped → PlanningWithClient → InTesting no es directo.
        // Ruta: Stopped → PlanningWithClient → PlanningSprint → InSprint → InTesting → Completed
        project.TransitionTo(ProjectStatus.PlanningWithClient);
        project.TransitionTo(ProjectStatus.PlanningSprint);
        project.TransitionTo(ProjectStatus.InSprint);
        project.TransitionTo(ProjectStatus.InTesting);
        await db.SaveChangesAsync();

        var cmd = new TransitionProjectStatusCommand(project.Id, ProjectStatus.Completed, gestor.Id);

        await handler.Handle(cmd, CancellationToken.None);

        var updated = await db.Projects.FindAsync(project.Id);
        updated!.Status.ShouldBe(ProjectStatus.Completed);
    }

    [Fact]
    public async Task Handle_CompletedConSprintNoCompletado_ThrowsInvalidOperationException()
    {
        var (db, project, gestor) = await SetupProjectWithGestor();
        await using var _ = db;

        // Avanzar a InTesting (estado previo a Completed) por ruta válida
        project.TransitionTo(ProjectStatus.PlanningWithClient);
        project.TransitionTo(ProjectStatus.PlanningSprint);
        project.TransitionTo(ProjectStatus.InSprint);
        project.TransitionTo(ProjectStatus.InTesting);

        db.Sprints.Add(Sprint.Create(project.Id, "Sprint 1", null, null, null, null));
        await db.SaveChangesAsync();

        var handler = new TransitionProjectStatusHandler(new ProjectLifecycleService(db));
        var cmd = new TransitionProjectStatusCommand(project.Id, ProjectStatus.Completed, gestor.Id);

        await Should.ThrowAsync<InvalidOperationException>(
            () => handler.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_CompletedConWorkItemNoDone_ThrowsInvalidOperationException()
    {
        var (db, project, gestor) = await SetupProjectWithGestor();
        await using var _ = db;

        // Avanzar a InTesting (estado previo a Completed) por ruta válida
        project.TransitionTo(ProjectStatus.PlanningWithClient);
        project.TransitionTo(ProjectStatus.PlanningSprint);
        project.TransitionTo(ProjectStatus.InSprint);
        project.TransitionTo(ProjectStatus.InTesting);

        db.WorkItems.Add(WorkItem.Create(project.Id, "Tarea", null, WorkItemPriority.Medium, null, 0, null, false, null, null));
        await db.SaveChangesAsync();

        var handler = new TransitionProjectStatusHandler(new ProjectLifecycleService(db));
        var cmd = new TransitionProjectStatusCommand(project.Id, ProjectStatus.Completed, gestor.Id);

        await Should.ThrowAsync<InvalidOperationException>(
            () => handler.Handle(cmd, CancellationToken.None));
    }

    // ── Nueva regla: miembro de equipo asignado al proyecto PUEDE transicionar ──

    [Fact]
    public async Task Handle_MiembroEquipoAsignado_Succeeds()
    {
        await using var db = CreateInMemoryContext();

        // Persona con rol Desarrollador (no Gestor)
        var dev = Person.CreateFromClaims("sub-dev", "Dev User", "dev@test.com", PersonRole.Desarrollador);
        db.Persons.Add(dev);

        var project = Project.Create("Proyecto Test", null, "Unidad TIC", ProjectComplexity.VerySmall, null, null, null);
        db.Projects.Add(project);

        var team = Team.Create("Equipo Asignado", null, null);
        db.Teams.Add(team);

        await db.SaveChangesAsync();

        // El dev es miembro del equipo y el equipo está asignado al proyecto
        db.PersonTeamMemberships.Add(PersonTeamMembership.Create(dev.Id, team.Id));
        db.ProjectTeamAssignments.Add(ProjectTeamAssignment.Create(project.Id, team.Id, true));
        await db.SaveChangesAsync();

        var handler = new TransitionProjectStatusHandler(new ProjectLifecycleService(db));
        var cmd = new TransitionProjectStatusCommand(project.Id, ProjectStatus.PlanningWithClient, dev.Id);

        await handler.Handle(cmd, CancellationToken.None);

        var updated = await db.Projects.FindAsync(project.Id);
        updated!.Status.ShouldBe(ProjectStatus.PlanningWithClient);
    }

    [Fact]
    public async Task Handle_PersonaAjenaAlProyecto_ThrowsUnauthorizedAccessException()
    {
        await using var db = CreateInMemoryContext();

        // JefeEquipo que NO pertenece a ningún equipo del proyecto
        var jefe = Person.CreateFromClaims("sub-jefe", "Jefe User", "jefe@test.com", PersonRole.JefeEquipo);
        db.Persons.Add(jefe);

        var otherTeam = Team.Create("Otro Equipo", null, null);
        db.Teams.Add(otherTeam);

        var project = Project.Create("Proyecto Test", null, "Unidad TIC", ProjectComplexity.VerySmall, null, null, null);
        db.Projects.Add(project);

        await db.SaveChangesAsync();

        // El jefe pertenece a un equipo que NO está asignado al proyecto
        db.PersonTeamMemberships.Add(PersonTeamMembership.Create(jefe.Id, otherTeam.Id));
        await db.SaveChangesAsync();

        var handler = new TransitionProjectStatusHandler(new ProjectLifecycleService(db));
        var cmd = new TransitionProjectStatusCommand(project.Id, ProjectStatus.PlanningWithClient, jefe.Id);

        await Should.ThrowAsync<UnauthorizedAccessException>(
            () => handler.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_JefeEquipoMiembroEquipoAsignado_Succeeds()
    {
        await using var db = CreateInMemoryContext();

        var jefe = Person.CreateFromClaims("sub-jefe", "Jefe User", "jefe@test.com", PersonRole.JefeEquipo);
        db.Persons.Add(jefe);

        var team = Team.Create("Equipo Asignado", null, null); // no es líder, solo miembro
        db.Teams.Add(team);

        var project = Project.Create("Proyecto Test", null, "Unidad TIC", ProjectComplexity.VerySmall, null, null, null);
        db.Projects.Add(project);

        await db.SaveChangesAsync();

        db.ProjectTeamAssignments.Add(ProjectTeamAssignment.Create(project.Id, team.Id, true));
        db.PersonTeamMemberships.Add(PersonTeamMembership.Create(jefe.Id, team.Id));
        await db.SaveChangesAsync();

        var handler = new TransitionProjectStatusHandler(new ProjectLifecycleService(db));
        var cmd = new TransitionProjectStatusCommand(project.Id, ProjectStatus.PlanningWithClient, jefe.Id);

        await handler.Handle(cmd, CancellationToken.None);

        var updated = await db.Projects.FindAsync(project.Id);
        updated!.Status.ShouldBe(ProjectStatus.PlanningWithClient);
    }

    [Fact]
    public async Task Handle_DesarrolladorSinEquipo_ThrowsUnauthorizedAccessException()
    {
        await using var db = CreateInMemoryContext();

        var dev = Person.CreateFromClaims("sub-dev", "Dev User", "dev@test.com", PersonRole.Desarrollador);
        db.Persons.Add(dev);

        var project = Project.Create("Proyecto Test", null, "Unidad TIC", ProjectComplexity.VerySmall, null, null, null);
        db.Projects.Add(project);

        await db.SaveChangesAsync();

        var handler = new TransitionProjectStatusHandler(new ProjectLifecycleService(db));
        var cmd = new TransitionProjectStatusCommand(project.Id, ProjectStatus.PlanningWithClient, dev.Id);

        await Should.ThrowAsync<UnauthorizedAccessException>(
            () => handler.Handle(cmd, CancellationToken.None));
    }

    // --- ProjectStatusHistory ---

    [Fact]
    public async Task TransitionProjectStatus_RecordsHistoryEntry()
    {
        var (db, project, gestor) = await SetupProjectWithGestor();
        await using var _ = db;

        var handler = new TransitionProjectStatusHandler(new ProjectLifecycleService(db));
        await handler.Handle(
            new TransitionProjectStatusCommand(project.Id, ProjectStatus.PlanningWithClient, gestor.Id),
            CancellationToken.None);

        var history = await db.ProjectStatusHistories.Where(h => h.ProjectId == project.Id).ToListAsync();
        history.Count.ShouldBe(1);
        history[0].FromStatus.ShouldBe(ProjectStatus.Stopped);
        history[0].ToStatus.ShouldBe(ProjectStatus.PlanningWithClient);
        history[0].ChangedById.ShouldBe(gestor.Id);
    }

    [Fact]
    public async Task TransitionProjectStatus_InvalidTransition_DoesNotRecordHistory()
    {
        var (db, project, gestor) = await SetupProjectWithGestor();
        await using var _ = db;

        // Avanzar a Completed por ruta válida para poder probar una transición inválida
        project.TransitionTo(ProjectStatus.PlanningWithClient);
        project.TransitionTo(ProjectStatus.PlanningSprint);
        project.TransitionTo(ProjectStatus.InSprint);
        project.TransitionTo(ProjectStatus.InTesting);
        project.TransitionTo(ProjectStatus.Completed);
        await db.SaveChangesAsync();

        var handler = new TransitionProjectStatusHandler(new ProjectLifecycleService(db));
        // Desde Completed no se puede ir a ningún estado
        await Should.ThrowAsync<InvalidOperationException>(
            () => handler.Handle(
                new TransitionProjectStatusCommand(project.Id, ProjectStatus.Stopped, gestor.Id),
                CancellationToken.None));

        // No debe haber entrada de histórico para la transición fallida (solo la de creación)
        var history = await db.ProjectStatusHistories.Where(h => h.ProjectId == project.Id).ToListAsync();
        history.ShouldBeEmpty(); // Ninguna porque Completed es terminal y no tenemos histórico previo en esta prueba
    }

    [Fact]
    public async Task TransitionProjectStatus_MultipleTransitions_RecordsAllHistoryEntries()
    {
        var (db, project, gestor) = await SetupProjectWithGestor();
        await using var _ = db;

        var handler = new TransitionProjectStatusHandler(new ProjectLifecycleService(db));

        // Primera transición
        await handler.Handle(
            new TransitionProjectStatusCommand(project.Id, ProjectStatus.PlanningWithClient, gestor.Id),
            CancellationToken.None);

        // Segunda transición
        await handler.Handle(
            new TransitionProjectStatusCommand(project.Id, ProjectStatus.PlanningSprint, gestor.Id),
            CancellationToken.None);

        var history = await db.ProjectStatusHistories
            .Where(h => h.ProjectId == project.Id)
            .OrderBy(h => h.ChangedAt)
            .ToListAsync();

        history.Count.ShouldBe(2);
        history[0].FromStatus.ShouldBe(ProjectStatus.Stopped);
        history[0].ToStatus.ShouldBe(ProjectStatus.PlanningWithClient);
        history[1].FromStatus.ShouldBe(ProjectStatus.PlanningWithClient);
        history[1].ToStatus.ShouldBe(ProjectStatus.PlanningSprint);
    }
}
