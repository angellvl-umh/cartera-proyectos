using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Features.Comments;
using CarteraProyectos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace CarteraProyectos.UnitTests.Features.Comments;

public class CommentHandlerTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static async Task<(AppDbContext db, Project project, WorkItem workItem)> DbWithWorkItem()
    {
        var db = CreateDb();
        var project = Project.Create("Proyecto Test", null, "TIC", ProjectComplexity.VerySmall, null, null, null);
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        var workItem = WorkItem.Create(project.Id, "Tarea Test", null, WorkItemPriority.Medium, null, 0, null, false, null, null);
        db.WorkItems.Add(workItem);
        await db.SaveChangesAsync();

        return (db, project, workItem);
    }

    private static async Task<(AppDbContext db, Project project, WorkItem workItem, Person person)> DbWithWorkItemAndPerson()
    {
        var (db, project, workItem) = await DbWithWorkItem();
        var person = Person.CreateFromClaims("test-sub", "Test User", "test@example.com", PersonRole.Desarrollador);
        db.Persons.Add(person);
        await db.SaveChangesAsync();

        return (db, project, workItem, person);
    }

    // --- GetComments ---

    [Fact]
    public async Task GetComments_WorkItemExists_ReturnsComments()
    {
        var (db, _, workItem, person) = await DbWithWorkItemAndPerson();
        var comment1 = Comment.Create(workItem.Id, person.Id, "Primer comentario");
        var comment2 = Comment.Create(workItem.Id, person.Id, "Segundo comentario");
        db.Comments.Add(comment1);
        db.Comments.Add(comment2);
        await db.SaveChangesAsync();

        var handler = new GetCommentsHandler(db);
        var result = await handler.Handle(
            new GetCommentsQuery(workItem.ProjectId, workItem.Id),
            CancellationToken.None);

        result.Count.ShouldBe(2);
        result[0].Text.ShouldBe("Primer comentario");
        result[1].Text.ShouldBe("Segundo comentario");
        result[0].CreatedAt.ShouldBeLessThanOrEqualTo(result[1].CreatedAt);
    }

    [Fact]
    public async Task GetComments_WorkItemFromOtherProject_ThrowsKeyNotFoundException()
    {
        var (db, project, workItem, _) = await DbWithWorkItemAndPerson();
        var otherProject = Project.Create("Otro", null, "TIC", ProjectComplexity.VerySmall, null, null, null);
        db.Projects.Add(otherProject);
        await db.SaveChangesAsync();

        var handler = new GetCommentsHandler(db);
        var ex = await Should.ThrowAsync<KeyNotFoundException>(async () =>
            await handler.Handle(new GetCommentsQuery(otherProject.Id, workItem.Id), CancellationToken.None));
        ex.Message.ShouldContain(workItem.Id.ToString());
    }

    // --- CreateComment ---

    [Fact]
    public async Task CreateComment_ValidCommand_CreatesAndReturnsId()
    {
        var (db, project, workItem, person) = await DbWithWorkItemAndPerson();
        var handler = new CreateCommentHandler(db);

        var id = await handler.Handle(
            new CreateCommentCommand(project.Id, workItem.Id, person.Id, "Test comment"),
            CancellationToken.None);

        id.ShouldBeGreaterThan(0);
        var comment = await db.Comments.FindAsync(id);
        comment.ShouldNotBeNull();
        comment.Text.ShouldBe("Test comment");
        comment.AuthorId.ShouldBe(person.Id);
    }

    [Fact]
    public async Task CreateComment_WorkItemNotFound_ThrowsKeyNotFoundException()
    {
        var (db, project, _, person) = await DbWithWorkItemAndPerson();
        var handler = new CreateCommentHandler(db);

        var ex = await Should.ThrowAsync<KeyNotFoundException>(async () =>
            await handler.Handle(
                new CreateCommentCommand(project.Id, 9999, person.Id, "Test"),
                CancellationToken.None));
        ex.Message.ShouldContain("9999");
    }

    [Fact]
    public async Task CreateComment_EmptyText_ThrowsValidationException()
    {
        var validator = new CreateCommentValidator();
        var cmd = new CreateCommentCommand(1, 1, 1, "");

        var result = await validator.ValidateAsync(cmd);
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public async Task CreateComment_TextExceedsMaxLength_ThrowsValidationException()
    {
        var validator = new CreateCommentValidator();
        var longText = new string('a', 2001);
        var cmd = new CreateCommentCommand(1, 1, 1, longText);

        var result = await validator.ValidateAsync(cmd);
        result.IsValid.ShouldBeFalse();
    }

    // --- DeleteComment ---

    [Fact]
    public async Task DeleteComment_OwnComment_Removes()
    {
        var (db, _, workItem, person) = await DbWithWorkItemAndPerson();
        var comment = Comment.Create(workItem.Id, person.Id, "To delete");
        db.Comments.Add(comment);
        await db.SaveChangesAsync();

        var handler = new DeleteCommentHandler(db);
        await handler.Handle(new DeleteCommentCommand(comment.Id, person.Id), CancellationToken.None);

        var deleted = await db.Comments.FindAsync(comment.Id);
        deleted.ShouldBeNull();
    }

    [Fact]
    public async Task DeleteComment_OtherPersonComment_ThrowsUnauthorizedAccessException()
    {
        var (db, _, workItem, person) = await DbWithWorkItemAndPerson();
        var other = Person.CreateFromClaims("other-sub", "Other", "other@test.com", PersonRole.Desarrollador);
        db.Persons.Add(other);
        await db.SaveChangesAsync();

        var comment = Comment.Create(workItem.Id, person.Id, "Not mine");
        db.Comments.Add(comment);
        await db.SaveChangesAsync();

        var handler = new DeleteCommentHandler(db);
        var ex = await Should.ThrowAsync<UnauthorizedAccessException>(async () =>
            await handler.Handle(new DeleteCommentCommand(comment.Id, other.Id), CancellationToken.None));
        ex.Message.ShouldContain("autor");
    }

    [Fact]
    public async Task DeleteComment_NotFound_ThrowsKeyNotFoundException()
    {
        var db = CreateDb();
        var handler = new DeleteCommentHandler(db);

        var ex = await Should.ThrowAsync<KeyNotFoundException>(async () =>
            await handler.Handle(new DeleteCommentCommand(9999, 1), CancellationToken.None));
        ex.Message.ShouldContain("9999");
    }
}
