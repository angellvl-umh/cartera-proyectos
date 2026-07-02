using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Features.Reports;
using CarteraProyectos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace CarteraProyectos.UnitTests.Features.Reports;

public class WeeklyPortfolioOpenHighImpactRisksTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static DateOnly GetMondayOfWeek(DateTime date)
    {
        var dateOnly = DateOnly.FromDateTime(date);
        var daysOfWeek = (int)dateOnly.DayOfWeek;
        var daysToMonday = daysOfWeek == 0 ? 6 : daysOfWeek - 1;
        return dateOnly.AddDays(-daysToMonday);
    }

    [Fact]
    public async Task OpenHighImpactRisks_CountsOnlyOpenAndHighImpact()
    {
        await using var db = CreateDb();

        var author = Person.CreateFromClaims("sub1", "Author", "author@test.com", PersonRole.Gestor);
        var project = Project.Create("Proyecto Riesgos", null, "TIC", ProjectComplexity.Small, 2026, null, null);
        project.TransitionTo(ProjectStatus.PlanningWithClient); // sacar de Stopped
        db.Persons.Add(author);
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        // 2 riesgos Open con Impact=High
        var r1 = ProjectRisk.Create(project.Id, "Riesgo 1", RiskLevel.High, RiskLevel.High, null, author.Id);
        var r2 = ProjectRisk.Create(project.Id, "Riesgo 2", RiskLevel.Medium, RiskLevel.High, null, author.Id);
        // 1 riesgo Closed con Impact=High (no debe contar)
        var r3 = ProjectRisk.Create(project.Id, "Riesgo 3", RiskLevel.High, RiskLevel.High, null, author.Id);
        r3.Update("Riesgo 3", RiskLevel.High, RiskLevel.High, null, RiskStatus.Closed);
        // 1 riesgo Open con Impact=Medium (no debe contar)
        var r4 = ProjectRisk.Create(project.Id, "Riesgo 4", RiskLevel.High, RiskLevel.Medium, null, author.Id);

        db.ProjectRisks.AddRange(r1, r2, r3, r4);

        // Update semanal para que el proyecto aparezca (si no tiene update esta semana → es "AtRisk")
        var monday = GetMondayOfWeek(DateTime.UtcNow);
        var update = ProjectWeeklyUpdate.Create(project.Id, author.Id, monday, "Summary", ProjectHealthStatus.OnTrack);
        db.ProjectWeeklyUpdates.Add(update);

        await db.SaveChangesAsync();

        var handler = new GetWeeklyPortfolioReportHandler(db);
        var result = await handler.Handle(new GetWeeklyPortfolioReportQuery(), CancellationToken.None);

        var projectDto = result.OtherProjects.SingleOrDefault(p => p.ProjectId == project.Id);
        projectDto.ShouldNotBeNull();
        projectDto.OpenHighImpactRisks.ShouldBe(2);
    }

    [Fact]
    public async Task OpenHighImpactRisks_NoRisks_ReturnsZero()
    {
        await using var db = CreateDb();

        var author = Person.CreateFromClaims("sub2", "Author2", "author2@test.com", PersonRole.Gestor);
        var project = Project.Create("Sin Riesgos", null, "TIC", ProjectComplexity.VerySmall, 2026, null, null);
        project.TransitionTo(ProjectStatus.PlanningWithClient);
        db.Persons.Add(author);
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        var monday = GetMondayOfWeek(DateTime.UtcNow);
        var update = ProjectWeeklyUpdate.Create(project.Id, author.Id, monday, "OK", ProjectHealthStatus.OnTrack);
        db.ProjectWeeklyUpdates.Add(update);
        await db.SaveChangesAsync();

        var handler = new GetWeeklyPortfolioReportHandler(db);
        var result = await handler.Handle(new GetWeeklyPortfolioReportQuery(), CancellationToken.None);

        var projectDto = result.OtherProjects.SingleOrDefault(p => p.ProjectId == project.Id);
        projectDto.ShouldNotBeNull();
        projectDto.OpenHighImpactRisks.ShouldBe(0);
    }

    [Fact]
    public async Task OpenHighImpactRisks_MultipleProjects_CountedIndependently()
    {
        await using var db = CreateDb();

        var author = Person.CreateFromClaims("sub3", "Author3", "author3@test.com", PersonRole.Gestor);
        var projX = Project.Create("Proyecto X", null, null, ProjectComplexity.Small, 2026, null, null);
        var projY = Project.Create("Proyecto Y", null, null, ProjectComplexity.Small, 2026, null, null);
        projX.TransitionTo(ProjectStatus.PlanningWithClient);
        projY.TransitionTo(ProjectStatus.PlanningWithClient);

        db.Persons.Add(author);
        db.Projects.AddRange(projX, projY);
        await db.SaveChangesAsync();

        // X tiene 3 riesgos open high-impact
        db.ProjectRisks.AddRange(
            ProjectRisk.Create(projX.Id, "RX1", RiskLevel.Low, RiskLevel.High, null, author.Id),
            ProjectRisk.Create(projX.Id, "RX2", RiskLevel.High, RiskLevel.High, null, author.Id),
            ProjectRisk.Create(projX.Id, "RX3", RiskLevel.Medium, RiskLevel.High, null, author.Id));

        // Y tiene 1 riesgo open high-impact
        db.ProjectRisks.Add(ProjectRisk.Create(projY.Id, "RY1", RiskLevel.Low, RiskLevel.High, null, author.Id));

        var monday = GetMondayOfWeek(DateTime.UtcNow);
        db.ProjectWeeklyUpdates.AddRange(
            ProjectWeeklyUpdate.Create(projX.Id, author.Id, monday, "OK", ProjectHealthStatus.OnTrack),
            ProjectWeeklyUpdate.Create(projY.Id, author.Id, monday, "OK", ProjectHealthStatus.OnTrack));
        await db.SaveChangesAsync();

        var handler = new GetWeeklyPortfolioReportHandler(db);
        var result = await handler.Handle(new GetWeeklyPortfolioReportQuery(), CancellationToken.None);

        var dtoX = result.OtherProjects.Single(p => p.ProjectId == projX.Id);
        var dtoY = result.OtherProjects.Single(p => p.ProjectId == projY.Id);

        dtoX.OpenHighImpactRisks.ShouldBe(3);
        dtoY.OpenHighImpactRisks.ShouldBe(1);
    }
}
