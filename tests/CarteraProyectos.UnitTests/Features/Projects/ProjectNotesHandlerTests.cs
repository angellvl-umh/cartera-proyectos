using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Features.Projects.Notes;
using CarteraProyectos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace CarteraProyectos.UnitTests.Features.Projects;

public class ProjectNotesHandlerTests
{
    private static AppDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static async Task<(AppDbContext db, Project project)> DbWithProject()
    {
        var db = CreateDb();
        var project = Project.Create("Proyecto Test", null, null, ProjectComplexity.Small, null, null, null);
        db.Projects.Add(project);
        await db.SaveChangesAsync();
        return (db, project);
    }

    private static async Task<Person> AddPersonAsync(AppDbContext db, PersonRole role)
    {
        var person = Person.CreateFromClaims(Guid.NewGuid().ToString(), "user", "user@uni.es", role);
        db.Persons.Add(person);
        await db.SaveChangesAsync();
        return person;
    }

    // --- GetProjectNotes ---

    [Fact]
    public async Task GetProjectNotes_ExistingProject_ReturnsNotesOrderedByDate()
    {
        var (db, project) = await DbWithProject();
        var author = await AddPersonAsync(db, PersonRole.Gestor);

        var note1 = ProjectNote.Create(project.Id, author.Id, "Primera nota");
        var note2 = ProjectNote.Create(project.Id, author.Id, "Segunda nota");
        db.ProjectNotes.AddRange(note1, note2);
        await db.SaveChangesAsync();

        var result = await new GetProjectNotesHandler(db).Handle(
            new GetProjectNotesQuery(project.Id), CancellationToken.None);

        result.Count.ShouldBe(2);
        result[0].Text.ShouldBe("Primera nota");
        result[1].Text.ShouldBe("Segunda nota");
    }

    [Fact]
    public async Task GetProjectNotes_ProjectNotFound_ThrowsKeyNotFoundException()
    {
        await using var db = CreateDb();

        await Should.ThrowAsync<KeyNotFoundException>(() =>
            new GetProjectNotesHandler(db).Handle(
                new GetProjectNotesQuery(999), CancellationToken.None));
    }

    // --- CreateProjectNote ---

    [Fact]
    public async Task CreateProjectNote_Gestor_CreatesNote()
    {
        var (db, project) = await DbWithProject();
        var gestor = await AddPersonAsync(db, PersonRole.Gestor);

        var id = await new CreateProjectNoteHandler(db).Handle(
            new CreateProjectNoteCommand(project.Id, gestor.Id, "Nota del gestor"), CancellationToken.None);

        id.ShouldBeGreaterThan(0);
        var note = await db.ProjectNotes.FindAsync(id);
        note.ShouldNotBeNull();
        note.Text.ShouldBe("Nota del gestor");
    }

    [Fact]
    public async Task CreateProjectNote_MiembroEquipoAsignado_CreatesNote()
    {
        var (db, project) = await DbWithProject();
        var jefe = await AddPersonAsync(db, PersonRole.JefeEquipo);

        var team = Team.Create("Equipo Asignado", null, null); // no es líder, solo miembro
        db.Teams.Add(team);
        await db.SaveChangesAsync();
        db.PersonTeamMemberships.Add(PersonTeamMembership.Create(jefe.Id, team.Id));
        db.ProjectTeamAssignments.Add(ProjectTeamAssignment.Create(project.Id, team.Id, true));
        await db.SaveChangesAsync();

        var id = await new CreateProjectNoteHandler(db).Handle(
            new CreateProjectNoteCommand(project.Id, jefe.Id, "Nota del miembro"), CancellationToken.None);

        id.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task CreateProjectNote_PersonaAjenaAlProyecto_ThrowsUnauthorizedAccessException()
    {
        var (db, project) = await DbWithProject();
        var jefe = await AddPersonAsync(db, PersonRole.JefeEquipo);

        // El jefe pertenece a un equipo pero ese equipo NO está asignado al proyecto
        var team = Team.Create("Equipo No Asignado", null, null);
        db.Teams.Add(team);
        await db.SaveChangesAsync();
        db.PersonTeamMemberships.Add(PersonTeamMembership.Create(jefe.Id, team.Id));
        await db.SaveChangesAsync();

        await Should.ThrowAsync<UnauthorizedAccessException>(() =>
            new CreateProjectNoteHandler(db).Handle(
                new CreateProjectNoteCommand(project.Id, jefe.Id, "Nota del jefe"), CancellationToken.None));
    }

    [Fact]
    public async Task CreateProjectNote_ProjectNotFound_ThrowsKeyNotFoundException()
    {
        await using var db = CreateDb();
        var gestor = await AddPersonAsync(db, PersonRole.Gestor);

        await Should.ThrowAsync<KeyNotFoundException>(() =>
            new CreateProjectNoteHandler(db).Handle(
                new CreateProjectNoteCommand(999, gestor.Id, "Texto"), CancellationToken.None));
    }

    [Fact]
    public async Task CreateProjectNote_DevSinEquipoAsignado_ThrowsUnauthorizedAccessException()
    {
        var (db, project) = await DbWithProject();
        var dev = await AddPersonAsync(db, PersonRole.Desarrollador);

        await Should.ThrowAsync<UnauthorizedAccessException>(() =>
            new CreateProjectNoteHandler(db).Handle(
                new CreateProjectNoteCommand(project.Id, dev.Id, "Texto"), CancellationToken.None));
    }

    // --- DeleteProjectNote ---

    [Fact]
    public async Task DeleteProjectNote_Author_DeletesNote()
    {
        var (db, project) = await DbWithProject();
        var gestor = await AddPersonAsync(db, PersonRole.Gestor);
        var note = ProjectNote.Create(project.Id, gestor.Id, "Mi nota");
        db.ProjectNotes.Add(note);
        await db.SaveChangesAsync();

        await new DeleteProjectNoteHandler(db).Handle(
            new DeleteProjectNoteCommand(project.Id, note.Id, gestor.Id), CancellationToken.None);

        (await db.ProjectNotes.FindAsync(note.Id)).ShouldBeNull();
    }

    [Fact]
    public async Task DeleteProjectNote_Gestor_CanDeleteOtherNote()
    {
        var (db, project) = await DbWithProject();
        var author = await AddPersonAsync(db, PersonRole.JefeEquipo);
        var gestor = await AddPersonAsync(db, PersonRole.Gestor);
        var note = ProjectNote.Create(project.Id, author.Id, "Nota del jefe");
        db.ProjectNotes.Add(note);
        await db.SaveChangesAsync();

        await new DeleteProjectNoteHandler(db).Handle(
            new DeleteProjectNoteCommand(project.Id, note.Id, gestor.Id), CancellationToken.None);

        (await db.ProjectNotes.FindAsync(note.Id)).ShouldBeNull();
    }

    [Fact]
    public async Task DeleteProjectNote_OtherPerson_ThrowsUnauthorizedAccessException()
    {
        var (db, project) = await DbWithProject();
        var author = await AddPersonAsync(db, PersonRole.JefeEquipo);
        var other = await AddPersonAsync(db, PersonRole.JefeEquipo);
        var note = ProjectNote.Create(project.Id, author.Id, "Nota");
        db.ProjectNotes.Add(note);
        await db.SaveChangesAsync();

        await Should.ThrowAsync<UnauthorizedAccessException>(() =>
            new DeleteProjectNoteHandler(db).Handle(
                new DeleteProjectNoteCommand(project.Id, note.Id, other.Id), CancellationToken.None));
    }

    [Fact]
    public async Task DeleteProjectNote_NotFound_ThrowsKeyNotFoundException()
    {
        var (db, project) = await DbWithProject();
        var gestor = await AddPersonAsync(db, PersonRole.Gestor);

        await Should.ThrowAsync<KeyNotFoundException>(() =>
            new DeleteProjectNoteHandler(db).Handle(
                new DeleteProjectNoteCommand(project.Id, 999, gestor.Id), CancellationToken.None));
    }
}
