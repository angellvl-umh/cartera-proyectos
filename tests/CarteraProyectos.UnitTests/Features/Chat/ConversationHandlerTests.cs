using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Features.Chat;
using CarteraProyectos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace CarteraProyectos.UnitTests.Features.Chat;

public class ConversationHandlerTests
{
    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static async Task<(AppDbContext db, Person user1, Person user2)> SetupTwoUsersAsync()
    {
        var db = CreateDb();

        var user1 = Person.CreateFromClaims("sub-1", "Usuario 1", "u1@example.com");
        var user2 = Person.CreateFromClaims("sub-2", "Usuario 2", "u2@example.com");
        db.Persons.AddRange(user1, user2);
        await db.SaveChangesAsync();

        return (db, user1, user2);
    }

    // ─── CreateConversationHandler ────────────────────────────────────────────

    [Fact]
    public async Task CreateConversation_ValidCommand_PersistsAndReturnsId()
    {
        var db = CreateDb();
        var person = Person.CreateFromClaims("sub-1", "Test", "test@example.com");
        db.Persons.Add(person);
        await db.SaveChangesAsync();

        var handler = new CreateConversationHandler(db);
        var id = await handler.Handle(
            new CreateConversationCommand(person.Id, "Mi conversación"),
            CancellationToken.None);

        id.ShouldBeGreaterThan(0);
        var conv = await db.Conversations.FindAsync(id);
        conv.ShouldNotBeNull();
        conv.Title.ShouldBe("Mi conversación");
        conv.PersonId.ShouldBe(person.Id);
    }

    // ─── GetConversationsHandler ──────────────────────────────────────────────

    [Fact]
    public async Task GetConversations_ReturnsOnlyOwnConversations()
    {
        var (db, user1, user2) = await SetupTwoUsersAsync();

        db.Conversations.AddRange(
            Conversation.Create(user1.Id, "Conv A del usuario 1"),
            Conversation.Create(user1.Id, "Conv B del usuario 1"),
            Conversation.Create(user2.Id, "Conv del usuario 2"));
        await db.SaveChangesAsync();

        var handler = new GetConversationsHandler(db);
        var result = await handler.Handle(
            new GetConversationsQuery(user1.Id, 1, 20),
            CancellationToken.None);

        result.Total.ShouldBe(2);
        result.Items.Count.ShouldBe(2);
        result.Items.ShouldAllBe(c => c.Title.Contains("usuario 1"));
    }

    [Fact]
    public async Task GetConversations_Paginates_Correctly()
    {
        var db = CreateDb();
        var person = Person.CreateFromClaims("sub-1", "Test", "test@example.com");
        db.Persons.Add(person);
        await db.SaveChangesAsync();

        for (int i = 0; i < 5; i++)
            db.Conversations.Add(Conversation.Create(person.Id, $"Conv {i}"));
        await db.SaveChangesAsync();

        var handler = new GetConversationsHandler(db);
        var result = await handler.Handle(
            new GetConversationsQuery(person.Id, Page: 1, PageSize: 2),
            CancellationToken.None);

        result.Total.ShouldBe(5);
        result.Items.Count.ShouldBe(2);
        result.Page.ShouldBe(1);
        result.PageSize.ShouldBe(2);
    }

    // ─── GetConversationMessagesHandler ──────────────────────────────────────

    [Fact]
    public async Task GetConversationMessages_OwnConversation_ReturnsMessages()
    {
        var db = CreateDb();
        var person = Person.CreateFromClaims("sub-1", "Test", "test@example.com");
        db.Persons.Add(person);
        await db.SaveChangesAsync();

        var conv = Conversation.Create(person.Id, "Test");
        db.Conversations.Add(conv);
        await db.SaveChangesAsync();

        db.ChatMessages.Add(ChatMessage.CreateUserMessage(conv.Id, "Hola"));
        db.ChatMessages.Add(ChatMessage.CreateAssistantMessage(conv.Id, "Hola, ¿en qué puedo ayudarte?"));
        await db.SaveChangesAsync();

        var handler = new GetConversationMessagesHandler(db);
        var messages = await handler.Handle(
            new GetConversationMessagesQuery(person.Id, conv.Id),
            CancellationToken.None);

        messages.Count.ShouldBe(2);
        messages[0].Role.ShouldBe("user");
        messages[1].Role.ShouldBe("assistant");
    }

    [Fact]
    public async Task GetConversationMessages_OtherUserConversation_ThrowsKeyNotFound()
    {
        var (db, user1, user2) = await SetupTwoUsersAsync();

        var conv = Conversation.Create(user1.Id, "Privada");
        db.Conversations.Add(conv);
        await db.SaveChangesAsync();

        var handler = new GetConversationMessagesHandler(db);

        // user2 intenta leer la conversación de user1
        await Should.ThrowAsync<KeyNotFoundException>(async () =>
            await handler.Handle(
                new GetConversationMessagesQuery(user2.Id, conv.Id),
                CancellationToken.None));
    }

    [Fact]
    public async Task GetConversationMessages_NonExistent_ThrowsKeyNotFound()
    {
        var db = CreateDb();
        var person = Person.CreateFromClaims("sub-1", "Test", "test@example.com");
        db.Persons.Add(person);
        await db.SaveChangesAsync();

        var handler = new GetConversationMessagesHandler(db);

        await Should.ThrowAsync<KeyNotFoundException>(async () =>
            await handler.Handle(
                new GetConversationMessagesQuery(person.Id, 99999),
                CancellationToken.None));
    }

    // ─── DeleteConversationHandler ────────────────────────────────────────────

    [Fact]
    public async Task DeleteConversation_OwnConversation_DeletesIt()
    {
        var db = CreateDb();
        var person = Person.CreateFromClaims("sub-1", "Test", "test@example.com");
        db.Persons.Add(person);
        await db.SaveChangesAsync();

        var conv = Conversation.Create(person.Id, "Para borrar");
        db.Conversations.Add(conv);
        await db.SaveChangesAsync();
        var convId = conv.Id;

        var handler = new DeleteConversationHandler(db);
        await handler.Handle(
            new DeleteConversationCommand(person.Id, convId),
            CancellationToken.None);

        var exists = await db.Conversations.AnyAsync(c => c.Id == convId);
        exists.ShouldBeFalse();
    }

    [Fact]
    public async Task DeleteConversation_OtherUserConversation_ThrowsKeyNotFound()
    {
        var (db, user1, user2) = await SetupTwoUsersAsync();

        var conv = Conversation.Create(user1.Id, "Privada");
        db.Conversations.Add(conv);
        await db.SaveChangesAsync();

        var handler = new DeleteConversationHandler(db);

        // user2 intenta borrar la conversación de user1
        await Should.ThrowAsync<KeyNotFoundException>(async () =>
            await handler.Handle(
                new DeleteConversationCommand(user2.Id, conv.Id),
                CancellationToken.None));

        // La conversación debe seguir existiendo
        var exists = await db.Conversations.AnyAsync(c => c.Id == conv.Id);
        exists.ShouldBeTrue();
    }

    [Fact]
    public async Task DeleteConversation_NonExistent_ThrowsKeyNotFound()
    {
        var db = CreateDb();
        var person = Person.CreateFromClaims("sub-1", "Test", "test@example.com");
        db.Persons.Add(person);
        await db.SaveChangesAsync();

        var handler = new DeleteConversationHandler(db);

        await Should.ThrowAsync<KeyNotFoundException>(async () =>
            await handler.Handle(
                new DeleteConversationCommand(person.Id, 99999),
                CancellationToken.None));
    }
}
