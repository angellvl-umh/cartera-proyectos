using System.Reflection;
using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Features.Activity;
using CarteraProyectos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace CarteraProyectos.UnitTests.Features.Activity;

public class GetActivityFeedHandlerTests
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

    private static Project MakeProject(string title = "Proyecto Test")
        => Project.Create(title, null, "TIC", ProjectComplexity.VerySmall, 2026, null, null);

    private static WorkItem MakeWorkItem(int projectId, string title = "Tarea Test")
        => WorkItem.Create(projectId, title, null, WorkItemPriority.Medium, null, 0, null, false, null, null);

    /// <summary>
    /// Fija por reflexión la fecha de un evento cuya entidad no admite pasarla por factory
    /// (ProjectStatusHistory.ChangedAt, Comment.CreatedAt, ProjectWeeklyUpdate.CreatedAt).
    /// Necesario en tests para controlar el orden cronológico de forma determinista.
    /// </summary>
    private static void SetDate(object entity, string property, object value)
    {
        var prop = entity.GetType().GetProperty(property, BindingFlags.Public | BindingFlags.Instance)!;
        // La propiedad tiene setter privado; usar el setter no público.
        prop.SetValue(entity, value);
    }

    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Feed_WithFiveEventTypes_ReturnsAllMergedAndOrderedByDateDesc()
    {
        await using var db = CreateDb();
        var person = MakePerson();
        var project = MakeProject();
        db.Persons.Add(person);
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        var wi = MakeWorkItem(project.Id);
        db.WorkItems.Add(wi);
        await db.SaveChangesAsync();

        var t1 = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        var t2 = new DateTime(2026, 1, 2, 10, 0, 0, DateTimeKind.Utc);
        var t3 = new DateTime(2026, 1, 3, 10, 0, 0, DateTimeKind.Utc);
        var t4 = new DateTime(2026, 1, 4, 10, 0, 0, DateTimeKind.Utc);
        var t5 = new DateTime(2026, 1, 5, 10, 0, 0, DateTimeKind.Utc);

        // Cambio de estado de proyecto (t2)
        var statusChange = ProjectStatusHistory.Create(project, ProjectStatus.PlanningWithClient, ProjectStatus.PlanningSprint, person.Id);
        SetDate(statusChange, nameof(ProjectStatusHistory.ChangedAt), t2);
        db.ProjectStatusHistories.Add(statusChange);

        // Tarea creada (t1)
        db.WorkItemStatusHistories.Add(WorkItemStatusHistory.Create(wi, null, WorkItemStatus.Backlog, person.Id, t1));
        // Tarea completada (t4)
        db.WorkItemStatusHistories.Add(WorkItemStatusHistory.Create(wi, WorkItemStatus.InProgress, WorkItemStatus.Done, person.Id, t4));

        // Comentario (t3)
        var comment = Comment.Create(wi.Id, person.Id, "Un comentario");
        SetDate(comment, nameof(Comment.CreatedAt), t3);
        db.Comments.Add(comment);

        // Avance semanal (t5)
        var weekly = ProjectWeeklyUpdate.Create(project.Id, person.Id, new DateOnly(2026, 1, 5), "Avance de la semana", ProjectHealthStatus.OnTrack);
        SetDate(weekly, nameof(ProjectWeeklyUpdate.CreatedAt), new DateTimeOffset(t5));
        db.ProjectWeeklyUpdates.Add(weekly);

        await db.SaveChangesAsync();

        var handler = new GetActivityFeedHandler(db);
        var result = await handler.Handle(new GetActivityFeedQuery(), CancellationToken.None);

        result.Total.ShouldBe(5);
        result.Items.Count.ShouldBe(5);
        result.Items.Select(i => i.Type).ShouldBe(new[]
        {
            "WeeklyUpdateRegistered",  // t5
            "WorkItemCompleted",       // t4
            "CommentAdded",            // t3
            "ProjectStatusChanged",    // t2
            "WorkItemCreated",         // t1
        });
    }

    [Fact]
    public async Task Feed_ExcludesProjectCreationEntryWithNullFromStatus()
    {
        await using var db = CreateDb();
        var person = MakePerson();
        var project = MakeProject();
        db.Persons.Add(person);
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        // Entrada de creación (FromStatus == null) — NO debe aparecer
        var creation = ProjectStatusHistory.Create(project, null, ProjectStatus.Stopped, person.Id);
        SetDate(creation, nameof(ProjectStatusHistory.ChangedAt), new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc));
        db.ProjectStatusHistories.Add(creation);

        // Cambio real de estado — sí aparece
        var change = ProjectStatusHistory.Create(project, ProjectStatus.Stopped, ProjectStatus.PlanningWithClient, person.Id);
        SetDate(change, nameof(ProjectStatusHistory.ChangedAt), new DateTime(2026, 1, 2, 10, 0, 0, DateTimeKind.Utc));
        db.ProjectStatusHistories.Add(change);

        await db.SaveChangesAsync();

        var handler = new GetActivityFeedHandler(db);
        var result = await handler.Handle(new GetActivityFeedQuery(), CancellationToken.None);

        result.Total.ShouldBe(1);
        result.Items.Count.ShouldBe(1);
        result.Items[0].Type.ShouldBe("ProjectStatusChanged");
        result.Items[0].Summary.ShouldBe("De Stopped a PlanningWithClient");
    }

    [Fact]
    public async Task Feed_FilterByProject_IncludesCommentsOfThatProjectWorkItems()
    {
        await using var db = CreateDb();
        var person = MakePerson();
        var projectA = MakeProject("Proyecto A");
        var projectB = MakeProject("Proyecto B");
        db.Persons.Add(person);
        db.Projects.AddRange(projectA, projectB);
        await db.SaveChangesAsync();

        var wiA = MakeWorkItem(projectA.Id, "Tarea A");
        var wiB = MakeWorkItem(projectB.Id, "Tarea B");
        db.WorkItems.AddRange(wiA, wiB);
        await db.SaveChangesAsync();

        // Comentario en tarea de A y en tarea de B
        db.Comments.Add(Comment.Create(wiA.Id, person.Id, "Comentario A"));
        db.Comments.Add(Comment.Create(wiB.Id, person.Id, "Comentario B"));
        // Tareas creadas en ambos proyectos
        db.WorkItemStatusHistories.Add(WorkItemStatusHistory.Create(wiA, null, WorkItemStatus.Backlog, person.Id, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
        db.WorkItemStatusHistories.Add(WorkItemStatusHistory.Create(wiB, null, WorkItemStatus.Backlog, person.Id, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
        await db.SaveChangesAsync();

        var handler = new GetActivityFeedHandler(db);
        var result = await handler.Handle(new GetActivityFeedQuery(ProjectId: projectA.Id), CancellationToken.None);

        result.Total.ShouldBe(2); // 1 comentario + 1 tarea creada, ambos de A
        result.Items.ShouldAllBe(i => i.ProjectId == projectA.Id);
        result.Items.ShouldContain(i => i.Type == "CommentAdded" && i.Summary == "Comentario A");
        result.Items.ShouldContain(i => i.Type == "WorkItemCreated" && i.Summary == "Tarea A");
    }

    [Fact]
    public async Task Feed_FilterByTeam_ReturnsOnlyEventsOfProjectsWithThatTeam()
    {
        await using var db = CreateDb();
        var person = MakePerson();
        var team = Team.Create("Equipo 1", null, null);
        var projectA = MakeProject("Proyecto A");
        var projectB = MakeProject("Proyecto B");
        db.Persons.Add(person);
        db.Teams.Add(team);
        db.Projects.AddRange(projectA, projectB);
        await db.SaveChangesAsync();

        // El equipo está asignado solo al proyecto A
        db.ProjectTeamAssignments.Add(ProjectTeamAssignment.Create(projectA.Id, team.Id, true));
        await db.SaveChangesAsync();

        var wiA = MakeWorkItem(projectA.Id, "Tarea A");
        var wiB = MakeWorkItem(projectB.Id, "Tarea B");
        db.WorkItems.AddRange(wiA, wiB);
        await db.SaveChangesAsync();

        db.WorkItemStatusHistories.Add(WorkItemStatusHistory.Create(wiA, null, WorkItemStatus.Backlog, person.Id, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
        db.WorkItemStatusHistories.Add(WorkItemStatusHistory.Create(wiB, null, WorkItemStatus.Backlog, person.Id, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
        await db.SaveChangesAsync();

        var handler = new GetActivityFeedHandler(db);
        var result = await handler.Handle(new GetActivityFeedQuery(TeamId: team.Id), CancellationToken.None);

        result.Total.ShouldBe(1);
        result.Items.Count.ShouldBe(1);
        result.Items[0].ProjectId.ShouldBe(projectA.Id);
    }

    [Fact]
    public async Task Feed_FilterByPerson_ReturnsOnlyEventsWhereThatPersonIsActor()
    {
        await using var db = CreateDb();
        var alice = MakePerson("Alice");
        var bob = MakePerson("Bob");
        var project = MakeProject();
        db.Persons.AddRange(alice, bob);
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        var wi = MakeWorkItem(project.Id);
        db.WorkItems.Add(wi);
        await db.SaveChangesAsync();

        // Alice crea la tarea; Bob la completa
        db.WorkItemStatusHistories.Add(WorkItemStatusHistory.Create(wi, null, WorkItemStatus.Backlog, alice.Id, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
        db.WorkItemStatusHistories.Add(WorkItemStatusHistory.Create(wi, WorkItemStatus.InProgress, WorkItemStatus.Done, bob.Id, new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc)));
        await db.SaveChangesAsync();

        var handler = new GetActivityFeedHandler(db);
        var result = await handler.Handle(new GetActivityFeedQuery(PersonId: alice.Id), CancellationToken.None);

        result.Total.ShouldBe(1);
        result.Items.Count.ShouldBe(1);
        result.Items[0].ActorId.ShouldBe(alice.Id);
        result.Items[0].Type.ShouldBe("WorkItemCreated");
    }

    [Fact]
    public async Task Feed_CombinedFilters_ProjectAndPerson_AppliesBoth()
    {
        await using var db = CreateDb();
        var alice = MakePerson("Alice");
        var bob = MakePerson("Bob");
        var projectA = MakeProject("Proyecto A");
        var projectB = MakeProject("Proyecto B");
        db.Persons.AddRange(alice, bob);
        db.Projects.AddRange(projectA, projectB);
        await db.SaveChangesAsync();

        var wiA = MakeWorkItem(projectA.Id, "Tarea A");
        var wiB = MakeWorkItem(projectB.Id, "Tarea B");
        db.WorkItems.AddRange(wiA, wiB);
        await db.SaveChangesAsync();

        // Alice en A (cumple ambos), Alice en B (falla proyecto), Bob en A (falla persona)
        db.WorkItemStatusHistories.Add(WorkItemStatusHistory.Create(wiA, null, WorkItemStatus.Backlog, alice.Id, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
        db.WorkItemStatusHistories.Add(WorkItemStatusHistory.Create(wiB, null, WorkItemStatus.Backlog, alice.Id, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
        db.WorkItemStatusHistories.Add(WorkItemStatusHistory.Create(wiA, null, WorkItemStatus.Backlog, bob.Id, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
        await db.SaveChangesAsync();

        var handler = new GetActivityFeedHandler(db);
        var result = await handler.Handle(new GetActivityFeedQuery(ProjectId: projectA.Id, PersonId: alice.Id), CancellationToken.None);

        result.Total.ShouldBe(1);
        result.Items.Count.ShouldBe(1);
        result.Items[0].ProjectId.ShouldBe(projectA.Id);
        result.Items[0].ActorId.ShouldBe(alice.Id);
    }

    [Fact]
    public async Task Feed_Pagination_SecondPageAndTotalReflectsFilters()
    {
        await using var db = CreateDb();
        var person = MakePerson();
        var project = MakeProject();
        db.Persons.Add(person);
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        var wi = MakeWorkItem(project.Id);
        db.WorkItems.Add(wi);
        await db.SaveChangesAsync();

        // 5 comentarios con fechas decrecientes controladas
        for (var i = 0; i < 5; i++)
        {
            var c = Comment.Create(wi.Id, person.Id, $"Comentario {i}");
            SetDate(c, nameof(Comment.CreatedAt), new DateTime(2026, 1, 1 + i, 0, 0, 0, DateTimeKind.Utc));
            db.Comments.Add(c);
        }
        await db.SaveChangesAsync();

        var handler = new GetActivityFeedHandler(db);
        var page2 = await handler.Handle(new GetActivityFeedQuery(Page: 2, PageSize: 2), CancellationToken.None);

        page2.Total.ShouldBe(5);
        page2.Page.ShouldBe(2);
        page2.PageSize.ShouldBe(2);
        page2.Items.Count.ShouldBe(2);
        // Orden desc por fecha: [Com4, Com3, Com2, Com1, Com0] → página 2 = [Com2, Com1]
        page2.Items[0].Summary.ShouldBe("Comentario 2");
        page2.Items[1].Summary.ShouldBe("Comentario 1");
    }

    [Fact]
    public async Task Feed_PageSizeAboveMax_IsClampedTo100()
    {
        await using var db = CreateDb();
        var handler = new GetActivityFeedHandler(db);

        var result = await handler.Handle(new GetActivityFeedQuery(PageSize: 500), CancellationToken.None);

        result.PageSize.ShouldBe(100);
    }

    [Fact]
    public async Task Feed_CommentSummary_TruncatedTo140Chars()
    {
        await using var db = CreateDb();
        var person = MakePerson();
        var project = MakeProject();
        db.Persons.Add(person);
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        var wi = MakeWorkItem(project.Id);
        db.WorkItems.Add(wi);
        await db.SaveChangesAsync();

        var longText = new string('x', 200);
        db.Comments.Add(Comment.Create(wi.Id, person.Id, longText));
        await db.SaveChangesAsync();

        var handler = new GetActivityFeedHandler(db);
        var result = await handler.Handle(new GetActivityFeedQuery(), CancellationToken.None);

        var summary = result.Items.Single(i => i.Type == "CommentAdded").Summary;
        summary.Length.ShouldBe(141); // 140 chars + '…'
        summary.ShouldEndWith("…");
    }
}
