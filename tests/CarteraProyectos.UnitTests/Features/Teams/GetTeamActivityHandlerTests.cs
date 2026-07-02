using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Features.Teams;
using CarteraProyectos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace CarteraProyectos.UnitTests.Features.Teams;

public class GetTeamActivityHandlerTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static async Task<(AppDbContext db, Team team)> DbWithTeam(string teamName = "Equipo Alpha")
    {
        var db = CreateDb();
        var team = Team.Create(teamName, null, null);
        db.Teams.Add(team);
        await db.SaveChangesAsync();
        return (db, team);
    }

    private static Person MakePerson(string name, PersonRole role = PersonRole.Desarrollador)
        => Person.CreateFromClaims($"sub-{Guid.NewGuid()}", name, $"{name.ToLower()}@test.com", role);

    private static WorkItem MakeWorkItem(int projectId, string title, WorkItemStatus status, WorkItemPriority priority = WorkItemPriority.Medium)
    {
        var wi = WorkItem.Create(projectId, title, null, priority, null, 0, null, false, null, null);
        if (status != WorkItemStatus.Backlog)
            wi.TransitionStatus(status);
        return wi;
    }

    // ── Persona con tareas activas y persona disponible ───────────────────────

    [Fact]
    public async Task GetTeamActivity_PersonWithTasksAndAvailablePerson_OrderAndGroupingCorrect()
    {
        var (db, team) = await DbWithTeam();

        var dev1 = MakePerson("Ana");      // tendrá 2 tareas activas
        var dev2 = MakePerson("Bruno");    // sin tareas (disponible)
        db.Persons.AddRange(dev1, dev2);
        await db.SaveChangesAsync();

        db.PersonTeamMemberships.Add(PersonTeamMembership.Create(dev1.Id, team.Id));
        db.PersonTeamMemberships.Add(PersonTeamMembership.Create(dev2.Id, team.Id));

        var project = Project.Create("Proyecto X", null, "TIC", ProjectComplexity.VerySmall, null, null, null);
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        var wi1 = MakeWorkItem(project.Id, "Tarea 1", WorkItemStatus.InProgress);
        var wi2 = MakeWorkItem(project.Id, "Tarea 2", WorkItemStatus.InProgress);
        db.WorkItems.AddRange(wi1, wi2);
        await db.SaveChangesAsync();

        wi1.AddAssignee(dev1);
        wi2.AddAssignee(dev1);
        await db.SaveChangesAsync();

        var handler = new GetTeamActivityHandler(db);
        var result = await handler.Handle(new GetTeamActivityQuery(), CancellationToken.None);

        result.Count.ShouldBe(1);
        var teamDto = result[0];
        teamDto.Members.Count.ShouldBe(2);

        // Ana (con tareas) debe ir primero
        teamDto.Members[0].Name.ShouldBe("Ana");
        teamDto.Members[0].ActiveTasks.Count.ShouldBe(2);

        // Bruno (disponible) va después
        teamDto.Members[1].Name.ShouldBe("Bruno");
        teamDto.Members[1].ActiveTasks.Count.ShouldBe(0);
    }

    // ── Blocked antes que InProgress ──────────────────────────────────────────

    [Fact]
    public async Task GetTeamActivity_BlockedTaskBeforeInProgress()
    {
        var (db, team) = await DbWithTeam();

        var dev = MakePerson("Carlos");
        db.Persons.Add(dev);
        await db.SaveChangesAsync();

        db.PersonTeamMemberships.Add(PersonTeamMembership.Create(dev.Id, team.Id));

        var project = Project.Create("Proyecto Y", null, "TIC", ProjectComplexity.VerySmall, null, null, null);
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        var wiInProgress = MakeWorkItem(project.Id, "En progreso", WorkItemStatus.InProgress);
        var wiBlocked    = MakeWorkItem(project.Id, "Bloqueada", WorkItemStatus.Blocked);
        db.WorkItems.AddRange(wiInProgress, wiBlocked);
        await db.SaveChangesAsync();

        wiInProgress.AddAssignee(dev);
        wiBlocked.AddAssignee(dev);
        await db.SaveChangesAsync();

        var handler = new GetTeamActivityHandler(db);
        var result = await handler.Handle(new GetTeamActivityQuery(), CancellationToken.None);

        var tasks = result[0].Members[0].ActiveTasks;
        tasks.Count.ShouldBe(2);
        // Blocked primero
        tasks[0].Status.ShouldBe("Blocked");
        tasks[1].Status.ShouldBe("InProgress");
    }

    // ── Done y Discarded no aparecen ─────────────────────────────────────────

    [Fact]
    public async Task GetTeamActivity_DoneAndDiscardedTasks_DoNotAppear()
    {
        var (db, team) = await DbWithTeam();

        var dev = MakePerson("Diana");
        db.Persons.Add(dev);
        await db.SaveChangesAsync();

        db.PersonTeamMemberships.Add(PersonTeamMembership.Create(dev.Id, team.Id));

        var project = Project.Create("Proyecto Z", null, "TIC", ProjectComplexity.VerySmall, null, null, null);
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        var wiDone      = MakeWorkItem(project.Id, "Completada", WorkItemStatus.Done);
        var wiDiscarded = MakeWorkItem(project.Id, "Descartada", WorkItemStatus.Discarded);
        var wiActive    = MakeWorkItem(project.Id, "Activa", WorkItemStatus.InProgress);
        db.WorkItems.AddRange(wiDone, wiDiscarded, wiActive);
        await db.SaveChangesAsync();

        wiDone.AddAssignee(dev);
        wiDiscarded.AddAssignee(dev);
        wiActive.AddAssignee(dev);
        await db.SaveChangesAsync();

        var handler = new GetTeamActivityHandler(db);
        var result = await handler.Handle(new GetTeamActivityQuery(), CancellationToken.None);

        var tasks = result[0].Members[0].ActiveTasks;
        tasks.Count.ShouldBe(1);
        tasks[0].Title.ShouldBe("Activa");
    }

    // ── Equipos ordenados por nombre ──────────────────────────────────────────

    [Fact]
    public async Task GetTeamActivity_TeamsOrderedByName()
    {
        var db = CreateDb();
        db.Teams.Add(Team.Create("Zebra Team", null, null));
        db.Teams.Add(Team.Create("Alpha Team", null, null));
        db.Teams.Add(Team.Create("Medio Team", null, null));
        await db.SaveChangesAsync();

        var handler = new GetTeamActivityHandler(db);
        var result = await handler.Handle(new GetTeamActivityQuery(), CancellationToken.None);

        result[0].TeamName.ShouldBe("Alpha Team");
        result[1].TeamName.ShouldBe("Medio Team");
        result[2].TeamName.ShouldBe("Zebra Team");
    }

    // ── Persona con más tareas primero dentro del equipo ─────────────────────

    [Fact]
    public async Task GetTeamActivity_PersonWithMoreTasksFirst()
    {
        var (db, team) = await DbWithTeam();

        var dev1 = MakePerson("Elena");  // 1 tarea
        var dev2 = MakePerson("Felipe"); // 3 tareas
        var dev3 = MakePerson("Gabi");   // 0 tareas
        db.Persons.AddRange(dev1, dev2, dev3);
        await db.SaveChangesAsync();

        db.PersonTeamMemberships.Add(PersonTeamMembership.Create(dev1.Id, team.Id));
        db.PersonTeamMemberships.Add(PersonTeamMembership.Create(dev2.Id, team.Id));
        db.PersonTeamMemberships.Add(PersonTeamMembership.Create(dev3.Id, team.Id));

        var project = Project.Create("Proyecto W", null, "TIC", ProjectComplexity.VerySmall, null, null, null);
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        var wi1 = MakeWorkItem(project.Id, "T1 Elena", WorkItemStatus.InProgress);
        var wi2 = MakeWorkItem(project.Id, "T1 Felipe", WorkItemStatus.InProgress);
        var wi3 = MakeWorkItem(project.Id, "T2 Felipe", WorkItemStatus.InProgress);
        var wi4 = MakeWorkItem(project.Id, "T3 Felipe", WorkItemStatus.Blocked);
        db.WorkItems.AddRange(wi1, wi2, wi3, wi4);
        await db.SaveChangesAsync();

        wi1.AddAssignee(dev1);
        wi2.AddAssignee(dev2);
        wi3.AddAssignee(dev2);
        wi4.AddAssignee(dev2);
        await db.SaveChangesAsync();

        var handler = new GetTeamActivityHandler(db);
        var result = await handler.Handle(new GetTeamActivityQuery(), CancellationToken.None);

        var members = result[0].Members;
        // Felipe (3 tareas) debe ir primero, luego Elena (1), luego Gabi (0)
        members[0].Name.ShouldBe("Felipe");
        members[0].ActiveTasks.Count.ShouldBe(3);
        members[1].Name.ShouldBe("Elena");
        members[1].ActiveTasks.Count.ShouldBe(1);
        members[2].Name.ShouldBe("Gabi");
        members[2].ActiveTasks.Count.ShouldBe(0);
    }
}
