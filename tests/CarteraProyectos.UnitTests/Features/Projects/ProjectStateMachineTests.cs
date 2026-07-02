using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Features.Projects;
using CarteraProyectos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace CarteraProyectos.UnitTests.Features.Projects;

/// <summary>
/// Tests para la máquina de estados de Project y el estado Discarded de WorkItem.
/// </summary>
public class ProjectStateMachineTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    // ── Project.TransitionTo ─────────────────────────────────────────────────

    [Fact]
    public void TransitionTo_ValidTransition_ChangesStatus()
    {
        var project = Project.Create("Test", null, "TIC", ProjectComplexity.VerySmall, null, null, null);
        project.Status.ShouldBe(ProjectStatus.Stopped);

        project.TransitionTo(ProjectStatus.PlanningWithClient);

        project.Status.ShouldBe(ProjectStatus.PlanningWithClient);
    }

    [Fact]
    public void TransitionTo_InvalidTransition_ThrowsInvalidOperationException()
    {
        var project = Project.Create("Test", null, "TIC", ProjectComplexity.VerySmall, null, null, null);
        // Stopped → InSprint no es una transición válida

        var ex = Should.Throw<InvalidOperationException>(() =>
            project.TransitionTo(ProjectStatus.InSprint));

        ex.Message.ShouldContain("Stopped");
        ex.Message.ShouldContain("InSprint");
    }

    [Fact]
    public void TransitionTo_FromCompleted_ThrowsInvalidOperationException()
    {
        // Completed es terminal: ninguna transición puede salir de él
        var project = Project.Create("Test", null, "TIC", ProjectComplexity.VerySmall, null, null, null);
        project.TransitionTo(ProjectStatus.PlanningWithClient);
        project.TransitionTo(ProjectStatus.PlanningSprint);
        project.TransitionTo(ProjectStatus.InSprint);
        project.TransitionTo(ProjectStatus.InTesting);
        project.TransitionTo(ProjectStatus.Completed);

        Should.Throw<InvalidOperationException>(() =>
            project.TransitionTo(ProjectStatus.Stopped));
    }

    [Theory]
    [InlineData(ProjectStatus.PlanningWithClient)]
    [InlineData(ProjectStatus.WaitingForDevelopers)]
    [InlineData(ProjectStatus.PlanningSprint)]
    [InlineData(ProjectStatus.InSprint)]
    [InlineData(ProjectStatus.DevelopmentOutsideSprint)]
    [InlineData(ProjectStatus.InTesting)]
    [InlineData(ProjectStatus.PostponedByClient)]
    public void TransitionTo_FromAnyNonTerminalState_CanGoToStopped(ProjectStatus fromStatus)
    {
        var project = Project.Create("Test", null, "TIC", ProjectComplexity.VerySmall, null, null, null);
        project.SetStatusDirectly(fromStatus); // bypass para test

        project.TransitionTo(ProjectStatus.Stopped);

        project.Status.ShouldBe(ProjectStatus.Stopped);
    }

    [Theory]
    [InlineData(ProjectStatus.Stopped)]
    [InlineData(ProjectStatus.PlanningWithClient)]
    [InlineData(ProjectStatus.WaitingForDevelopers)]
    [InlineData(ProjectStatus.PlanningSprint)]
    [InlineData(ProjectStatus.InSprint)]
    [InlineData(ProjectStatus.DevelopmentOutsideSprint)]
    [InlineData(ProjectStatus.InTesting)]
    public void TransitionTo_FromMostNonTerminalStates_CanGoToPostponedByClient(ProjectStatus fromStatus)
    {
        var project = Project.Create("Test", null, "TIC", ProjectComplexity.VerySmall, null, null, null);
        project.SetStatusDirectly(fromStatus);

        project.TransitionTo(ProjectStatus.PostponedByClient);

        project.Status.ShouldBe(ProjectStatus.PostponedByClient);
    }

    // ── Project.GetAllowedTransitions ────────────────────────────────────────

    [Fact]
    public void GetAllowedTransitions_FromCompleted_ReturnsEmptyList()
    {
        var allowed = Project.GetAllowedTransitions(ProjectStatus.Completed);

        allowed.ShouldBeEmpty();
    }

    [Fact]
    public void GetAllowedTransitions_FromStopped_ReturnsPlanningWithClientAndPostponedByClient()
    {
        var allowed = Project.GetAllowedTransitions(ProjectStatus.Stopped);

        allowed.ShouldContain(ProjectStatus.PlanningWithClient);
        allowed.ShouldContain(ProjectStatus.PostponedByClient);
        allowed.Count.ShouldBe(2);
    }

    [Fact]
    public void GetAllowedTransitions_FromInTesting_IncludesCompleted()
    {
        var allowed = Project.GetAllowedTransitions(ProjectStatus.InTesting);

        allowed.ShouldContain(ProjectStatus.Completed);
    }

    // ── WorkItem.TransitionStatus con Discarded ──────────────────────────────

    [Fact]
    public void WorkItemTransitionStatus_ToDiscardedFromInProgress_Succeeds()
    {
        var wi = WorkItem.Create(1, "Tarea", null, WorkItemPriority.Medium, null, 0, null, false, null, null);
        wi.TransitionStatus(WorkItemStatus.InProgress);

        wi.TransitionStatus(WorkItemStatus.Discarded);

        wi.Status.ShouldBe(WorkItemStatus.Discarded);
    }

    [Fact]
    public void WorkItemTransitionStatus_FromDiscarded_ThrowsInvalidOperationException()
    {
        var wi = WorkItem.Create(1, "Tarea", null, WorkItemPriority.Medium, null, 0, null, false, null, null);
        wi.TransitionStatus(WorkItemStatus.Discarded);

        var ex = Should.Throw<InvalidOperationException>(() =>
            wi.TransitionStatus(WorkItemStatus.Backlog));

        ex.Message.ShouldContain("Discarded");
    }

    [Fact]
    public void WorkItemTransitionStatus_FromDone_StillThrowsInvalidOperationException()
    {
        var wi = WorkItem.Create(1, "Tarea", null, WorkItemPriority.Medium, null, 0, null, false, null, null);
        wi.TransitionStatus(WorkItemStatus.Done);

        var ex = Should.Throw<InvalidOperationException>(() =>
            wi.TransitionStatus(WorkItemStatus.InProgress));

        ex.Message.ShouldContain("Done");
    }

    [Theory]
    [InlineData(WorkItemStatus.Backlog)]
    [InlineData(WorkItemStatus.ToDo)]
    [InlineData(WorkItemStatus.InProgress)]
    [InlineData(WorkItemStatus.Blocked)]
    public void WorkItemTransitionStatus_ToDiscardedFromAnyNonTerminalStatus_Succeeds(WorkItemStatus fromStatus)
    {
        var wi = WorkItem.Create(1, "Tarea", null, WorkItemPriority.Medium, null, 0, null, false, null, null);
        if (fromStatus != WorkItemStatus.Backlog)
            wi.TransitionStatus(fromStatus);

        wi.TransitionStatus(WorkItemStatus.Discarded);

        wi.Status.ShouldBe(WorkItemStatus.Discarded);
    }

    // ── TransitionProjectStatusHandler: Discarded cuenta como terminado ──────

    [Fact]
    public async Task Handle_ProjectCompletedWithAllTasksDoneOrDiscarded_Succeeds()
    {
        await using var db = CreateDb();

        var gestor = Person.CreateFromClaims("sub-g", "Gestor", "g@test.com", PersonRole.Gestor);
        db.Persons.Add(gestor);

        var project = Project.Create("Proyecto", null, "TIC", ProjectComplexity.VerySmall, null, null, null);
        project.TransitionTo(ProjectStatus.PlanningWithClient);
        project.TransitionTo(ProjectStatus.PlanningSprint);
        project.TransitionTo(ProjectStatus.InSprint);
        project.TransitionTo(ProjectStatus.InTesting);
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        // Tareas: una Done y una Discarded → ambas son terminales, el proyecto puede completarse
        var taskDone = WorkItem.Create(project.Id, "Tarea Done", null, WorkItemPriority.Medium, null, 0, null, false, null, null);
        taskDone.TransitionStatus(WorkItemStatus.Done);
        var taskDiscarded = WorkItem.Create(project.Id, "Tarea Discarded", null, WorkItemPriority.Low, null, 1, null, false, null, null);
        taskDiscarded.TransitionStatus(WorkItemStatus.Discarded);
        db.WorkItems.AddRange(taskDone, taskDiscarded);
        await db.SaveChangesAsync();

        var handler = new TransitionProjectStatusHandler(db);
        await handler.Handle(
            new TransitionProjectStatusCommand(project.Id, ProjectStatus.Completed, gestor.Id),
            CancellationToken.None);

        var updated = await db.Projects.FindAsync(project.Id);
        updated!.Status.ShouldBe(ProjectStatus.Completed);
    }

    [Fact]
    public async Task Handle_ProjectCompletedWithPendingTask_ThrowsInvalidOperationException()
    {
        await using var db = CreateDb();

        var gestor = Person.CreateFromClaims("sub-g2", "Gestor2", "g2@test.com", PersonRole.Gestor);
        db.Persons.Add(gestor);

        var project = Project.Create("Proyecto", null, "TIC", ProjectComplexity.VerySmall, null, null, null);
        project.TransitionTo(ProjectStatus.PlanningWithClient);
        project.TransitionTo(ProjectStatus.PlanningSprint);
        project.TransitionTo(ProjectStatus.InSprint);
        project.TransitionTo(ProjectStatus.InTesting);
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        // Una tarea sigue en ToDo → no se puede completar el proyecto
        var taskToDo = WorkItem.Create(project.Id, "Tarea pendiente", null, WorkItemPriority.Medium, null, 0, null, false, null, null);
        taskToDo.TransitionStatus(WorkItemStatus.ToDo);
        var taskDone = WorkItem.Create(project.Id, "Tarea done", null, WorkItemPriority.Low, null, 1, null, false, null, null);
        taskDone.TransitionStatus(WorkItemStatus.Done);
        db.WorkItems.AddRange(taskToDo, taskDone);
        await db.SaveChangesAsync();

        var handler = new TransitionProjectStatusHandler(db);

        await Should.ThrowAsync<InvalidOperationException>(
            () => handler.Handle(
                new TransitionProjectStatusCommand(project.Id, ProjectStatus.Completed, gestor.Id),
                CancellationToken.None));
    }
}
