using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Features.Dashboard;
using CarteraProyectos.Core.Features.Persons;
using CarteraProyectos.Core.Features.Reports;
using CarteraProyectos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace CarteraProyectos.UnitTests.Features.Reports;

public class ReportHandlerTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    // ─── Seed helpers ────────────────────────────────────────────────────────

    private static Person MakePerson(string name = "Alice", PersonRole role = PersonRole.Desarrollador)
        => Person.CreateFromClaims(Guid.NewGuid().ToString(), name, $"{name.ToLower()}@test.com", role);

    private static Project MakeProject(string title = "Proyecto Test", ProjectStatus status = ProjectStatus.Stopped)
    {
        var p = Project.Create(title, null, "TIC", ProjectComplexity.VerySmall, 2026, null, null);
        if (status != ProjectStatus.Stopped) p.TransitionTo(status);
        return p;
    }

    private static async Task<(AppDbContext db, Person person, Project project, Team team)> SeedPersonInTeamWithProject(
        ProjectStatus projectStatus = ProjectStatus.InSprint)
    {
        var db = CreateDb();

        var person = MakePerson();
        var project = MakeProject(status: projectStatus);
        var team = Team.Create("Equipo A", null, null);

        db.Persons.Add(person);
        db.Projects.Add(project);
        db.Teams.Add(team);
        await db.SaveChangesAsync();

        db.PersonTeamMemberships.Add(PersonTeamMembership.Create(person.Id, team.Id));
        db.ProjectTeamAssignments.Add(ProjectTeamAssignment.Create(project.Id, team.Id, isPrimary: true));
        await db.SaveChangesAsync();

        return (db, person, project, team);
    }

    private static WorkItem MakeWorkItem(int projectId, Person? assignee = null,
        WorkItemStatus status = WorkItemStatus.Backlog, WorkItemPriority priority = WorkItemPriority.Medium)
    {
        var wi = WorkItem.Create(projectId, "Tarea", null, priority, null, 0, null, false, null, null);
        if (status != WorkItemStatus.Backlog) wi.TransitionStatus(status);
        if (assignee != null) ((List<Person>)wi.Assignees).Add(assignee);
        return wi;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GetDashboard
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetDashboard_PersonNotFound_ThrowsKeyNotFoundException()
    {
        await using var db = CreateDb();
        var handler = new GetDashboardHandler(db);

        await Should.ThrowAsync<KeyNotFoundException>(
            () => handler.Handle(new GetDashboardQuery(999), CancellationToken.None));
    }

    [Fact]
    public async Task GetDashboard_PersonWithNoTeams_ReturnsEmptyCollections()
    {
        await using var db = CreateDb();
        var person = MakePerson();
        db.Persons.Add(person);
        await db.SaveChangesAsync();

        var handler = new GetDashboardHandler(db);
        var result = await handler.Handle(new GetDashboardQuery(person.Id), CancellationToken.None);

        result.Me.Name.ShouldBe(person.Name);
        result.MyProjects.ShouldBeEmpty();
        result.ActiveSprints.ShouldBeEmpty();
        result.MyWorkItems.Total.ShouldBe(0);
    }

    [Fact]
    public async Task GetDashboard_PersonInTeamWithProject_ReturnsProjectsAndStats()
    {
        var (db, person, project, _) = await SeedPersonInTeamWithProject();
        var wi = MakeWorkItem(project.Id, person, WorkItemStatus.InProgress, WorkItemPriority.High);
        db.WorkItems.Add(wi);
        await db.SaveChangesAsync();

        var handler = new GetDashboardHandler(db);
        var result = await handler.Handle(new GetDashboardQuery(person.Id), CancellationToken.None);

        result.MyProjects.Count.ShouldBe(1);
        result.MyProjects[0].Title.ShouldBe(project.Title);
        result.MyWorkItems.Total.ShouldBe(1);
        result.MyWorkItems.InProgress.ShouldBe(1);
        result.MyWorkItems.High.ShouldBe(1);
    }

    [Fact]
    public async Task GetDashboard_ActiveSprintInProject_IncludedInResult()
    {
        var (db, person, project, _) = await SeedPersonInTeamWithProject();
        var sprint = Sprint.Create(project.Id, "Sprint 1", null, null, null, null);
        sprint.TransitionStatus(SprintStatus.Active);
        db.Sprints.Add(sprint);
        await db.SaveChangesAsync();

        var handler = new GetDashboardHandler(db);
        var result = await handler.Handle(new GetDashboardQuery(person.Id), CancellationToken.None);

        result.ActiveSprints.Count.ShouldBe(1);
        result.ActiveSprints[0].Name.ShouldBe("Sprint 1");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GetPortfolio
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetPortfolio_NoFilter_ReturnsAllProjects()
    {
        await using var db = CreateDb();
        db.Projects.Add(MakeProject("Proyecto A", ProjectStatus.InSprint));
        db.Projects.Add(MakeProject("Proyecto B", ProjectStatus.Completed));
        await db.SaveChangesAsync();

        var handler = new GetPortfolioHandler(db);
        var result = await handler.Handle(new GetPortfolioQuery(), CancellationToken.None);

        result.Projects.Count.ShouldBe(2);
        result.Stats.Total.ShouldBe(2);
        result.Stats.InSprint.ShouldBe(1);
        result.Stats.Completed.ShouldBe(1);
    }

    [Fact]
    public async Task GetPortfolio_FilterByStatus_ReturnsOnlyMatchingProjects()
    {
        await using var db = CreateDb();
        db.Projects.Add(MakeProject("Proyecto A", ProjectStatus.InSprint));
        db.Projects.Add(MakeProject("Proyecto B", ProjectStatus.Completed));
        await db.SaveChangesAsync();

        var handler = new GetPortfolioHandler(db);
        var result = await handler.Handle(new GetPortfolioQuery(Status: "InSprint"), CancellationToken.None);

        result.Projects.Count.ShouldBe(1);
        result.Projects[0].Title.ShouldBe("Proyecto A");
    }

    [Fact]
    public async Task GetPortfolio_FilterByYear_ReturnsOnlyMatchingYear()
    {
        await using var db = CreateDb();
        var p2025 = Project.Create("Proyecto 2025", null, "TIC", ProjectComplexity.VerySmall, 2025, null, null);
        var p2026 = Project.Create("Proyecto 2026", null, "TIC", ProjectComplexity.VerySmall, 2026, null, null);
        db.Projects.AddRange(p2025, p2026);
        await db.SaveChangesAsync();

        var handler = new GetPortfolioHandler(db);
        var result = await handler.Handle(new GetPortfolioQuery(Year: 2025), CancellationToken.None);

        result.Projects.Count.ShouldBe(1);
        result.Projects[0].PortfolioYear.ShouldBe(2025);
    }

    [Fact]
    public async Task GetPortfolio_AvailableYears_ReturnsDistinctOrderedYears()
    {
        await using var db = CreateDb();
        db.Projects.Add(Project.Create("A", null, "TIC", ProjectComplexity.VerySmall, 2024, null, null));
        db.Projects.Add(Project.Create("B", null, "TIC", ProjectComplexity.VerySmall, 2026, null, null));
        db.Projects.Add(Project.Create("C", null, "TIC", ProjectComplexity.VerySmall, 2026, null, null));
        await db.SaveChangesAsync();

        var handler = new GetPortfolioHandler(db);
        var result = await handler.Handle(new GetPortfolioQuery(), CancellationToken.None);

        result.AvailableYears.ShouldBe([2026, 2024]);
    }

    [Fact]
    public async Task GetPortfolio_ProjectWithWorkItemsAndMilestones_CountsCorrectly()
    {
        await using var db = CreateDb();
        var project = MakeProject(status: ProjectStatus.InSprint);
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        var wi1 = WorkItem.Create(project.Id, "Tarea 1", null, WorkItemPriority.Medium, null, 0, null, false, null, null);
        var wi2 = WorkItem.Create(project.Id, "Hito", null, WorkItemPriority.High, null, 1, null, true, null, null);
        wi2.TransitionStatus(WorkItemStatus.Done);
        db.WorkItems.AddRange(wi1, wi2);
        await db.SaveChangesAsync();

        var handler = new GetPortfolioHandler(db);
        var result = await handler.Handle(new GetPortfolioQuery(), CancellationToken.None);

        var dto = result.Projects[0];
        dto.TotalWorkItems.ShouldBe(2);
        dto.DoneWorkItems.ShouldBe(1);
        dto.TotalMilestones.ShouldBe(1);
        dto.ReachedMilestones.ShouldBe(1);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GetMyWorkItems
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetMyWorkItems_NoAssignedTasks_ReturnsEmpty()
    {
        await using var db = CreateDb();
        var person = MakePerson();
        db.Persons.Add(person);
        await db.SaveChangesAsync();

        var handler = new GetMyWorkItemsHandler(db);
        var result = await handler.Handle(new GetMyWorkItemsQuery(person.Id), CancellationToken.None);

        result.Items.ShouldBeEmpty();
        result.Total.ShouldBe(0);
        result.Counts.Total.ShouldBe(0);
    }

    [Fact]
    public async Task GetMyWorkItems_FilterByStatus_ReturnsOnlyMatchingStatus()
    {
        await using var db = CreateDb();
        var person = MakePerson();
        var project = MakeProject();
        db.Persons.Add(person);
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        var wi1 = MakeWorkItem(project.Id, person, WorkItemStatus.InProgress);
        var wi2 = MakeWorkItem(project.Id, person, WorkItemStatus.ToDo);
        db.WorkItems.AddRange(wi1, wi2);
        await db.SaveChangesAsync();

        var handler = new GetMyWorkItemsHandler(db);
        var result = await handler.Handle(
            new GetMyWorkItemsQuery(person.Id, Status: WorkItemStatus.InProgress), CancellationToken.None);

        result.Items.Count.ShouldBe(1);
        result.Items[0].Status.ShouldBe("InProgress");
        result.Counts.Total.ShouldBe(2); // counts are always for all statuses
    }

    [Fact]
    public async Task GetMyWorkItems_OrdersPriorityBeforeDone()
    {
        await using var db = CreateDb();
        var person = MakePerson();
        var project = MakeProject();
        db.Persons.Add(person);
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        var done     = MakeWorkItem(project.Id, person, WorkItemStatus.Done, WorkItemPriority.Critical);
        var inProg   = MakeWorkItem(project.Id, person, WorkItemStatus.InProgress, WorkItemPriority.Low);
        db.WorkItems.AddRange(done, inProg);
        await db.SaveChangesAsync();

        var handler = new GetMyWorkItemsHandler(db);
        var result = await handler.Handle(new GetMyWorkItemsQuery(person.Id), CancellationToken.None);

        result.Items[0].Status.ShouldBe("InProgress"); // non-done first
        result.Items[1].Status.ShouldBe("Done");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GetPersonProfile
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetPersonProfile_PersonNotFound_ThrowsKeyNotFoundException()
    {
        await using var db = CreateDb();
        var handler = new GetPersonProfileHandler(db);

        await Should.ThrowAsync<KeyNotFoundException>(
            () => handler.Handle(new GetPersonProfileQuery(999), CancellationToken.None));
    }

    [Fact]
    public async Task GetPersonProfile_PersonWithNoData_ReturnsEmptyCollections()
    {
        await using var db = CreateDb();
        var person = MakePerson("Bob");
        db.Persons.Add(person);
        await db.SaveChangesAsync();

        var handler = new GetPersonProfileHandler(db);
        var result = await handler.Handle(new GetPersonProfileQuery(person.Id), CancellationToken.None);

        result.Name.ShouldBe("Bob");
        result.Teams.ShouldBeEmpty();
        result.Workload.Total.ShouldBe(0);
        result.ActiveTasks.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetPersonProfile_IsLead_DetectedCorrectly()
    {
        await using var db = CreateDb();
        var person = MakePerson("Lead");
        db.Persons.Add(person);
        await db.SaveChangesAsync();

        var team = Team.Create("Equipo", null, person.Id);
        db.Teams.Add(team);
        await db.SaveChangesAsync();

        db.PersonTeamMemberships.Add(PersonTeamMembership.Create(person.Id, team.Id));
        await db.SaveChangesAsync();

        var handler = new GetPersonProfileHandler(db);
        var result = await handler.Handle(new GetPersonProfileQuery(person.Id), CancellationToken.None);

        result.Teams.Count.ShouldBe(1);
        result.Teams[0].IsLead.ShouldBeTrue();
    }

    [Fact]
    public async Task GetPersonProfile_ActiveTasksExcludeBacklogAndDone()
    {
        await using var db = CreateDb();
        var person = MakePerson();
        var project = MakeProject();
        db.Persons.Add(person);
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        var backlog  = MakeWorkItem(project.Id, person, WorkItemStatus.Backlog);
        var inProg   = MakeWorkItem(project.Id, person, WorkItemStatus.InProgress);
        var done     = MakeWorkItem(project.Id, person, WorkItemStatus.Done);
        db.WorkItems.AddRange(backlog, inProg, done);
        await db.SaveChangesAsync();

        var handler = new GetPersonProfileHandler(db);
        var result = await handler.Handle(new GetPersonProfileQuery(person.Id), CancellationToken.None);

        result.Workload.Total.ShouldBe(3);
        result.ActiveTasks.Count.ShouldBe(1); // only InProgress
        result.ActiveTasks[0].Status.ShouldBe("InProgress");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GetCapacity
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetCapacity_NoTeams_ReturnsEmptyList()
    {
        await using var db = CreateDb();
        var handler = new GetCapacityHandler(db);

        var result = await handler.Handle(new GetCapacityQuery(), CancellationToken.None);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetCapacity_TeamWithMember_ReturnsCorrectLoadLevel()
    {
        await using var db = CreateDb();
        var person = MakePerson();
        var project = MakeProject(status: ProjectStatus.InSprint);
        var team = Team.Create("Dev Team", null, null);

        db.Persons.Add(person);
        db.Projects.Add(project);
        db.Teams.Add(team);
        await db.SaveChangesAsync();

        db.PersonTeamMemberships.Add(PersonTeamMembership.Create(person.Id, team.Id));
        db.ProjectTeamAssignments.Add(ProjectTeamAssignment.Create(project.Id, team.Id, isPrimary: true));
        await db.SaveChangesAsync();

        // 2 active tasks → Green (≤3)
        var wi1 = MakeWorkItem(project.Id, person, WorkItemStatus.InProgress);
        var wi2 = MakeWorkItem(project.Id, person, WorkItemStatus.InProgress);
        db.WorkItems.AddRange(wi1, wi2);
        await db.SaveChangesAsync();

        var handler = new GetCapacityHandler(db);
        var result = await handler.Handle(new GetCapacityQuery(), CancellationToken.None);

        result.Count.ShouldBe(1);
        var teamDto = result[0];
        teamDto.TeamName.ShouldBe("Dev Team");
        teamDto.ActiveProjectCount.ShouldBe(1);
        teamDto.Members.Count.ShouldBe(1);
        teamDto.Members[0].ActiveTasks.ShouldBe(2);
        teamDto.Members[0].LoadLevel.ShouldBe("Green");
    }

    [Fact]
    public async Task GetCapacity_MemberWithSevenActiveTasks_IsRed()
    {
        await using var db = CreateDb();
        var person = MakePerson();
        var project = MakeProject(status: ProjectStatus.InSprint);
        var team = Team.Create("Red Team", null, null);

        db.Persons.Add(person);
        db.Projects.Add(project);
        db.Teams.Add(team);
        await db.SaveChangesAsync();

        db.PersonTeamMemberships.Add(PersonTeamMembership.Create(person.Id, team.Id));
        await db.SaveChangesAsync();

        for (var i = 0; i < 7; i++)
            db.WorkItems.Add(MakeWorkItem(project.Id, person, WorkItemStatus.InProgress));
        await db.SaveChangesAsync();

        var handler = new GetCapacityHandler(db);
        var result = await handler.Handle(new GetCapacityQuery(), CancellationToken.None);

        result[0].Members[0].LoadLevel.ShouldBe("Red");
    }

    [Fact]
    public async Task GetCapacity_TeamsOrderedAlphabetically()
    {
        await using var db = CreateDb();
        db.Teams.Add(Team.Create("Zebra Team", null, null));
        db.Teams.Add(Team.Create("Alpha Team", null, null));
        await db.SaveChangesAsync();

        var handler = new GetCapacityHandler(db);
        var result = await handler.Handle(new GetCapacityQuery(), CancellationToken.None);

        result[0].TeamName.ShouldBe("Alpha Team");
        result[1].TeamName.ShouldBe("Zebra Team");
    }
}
