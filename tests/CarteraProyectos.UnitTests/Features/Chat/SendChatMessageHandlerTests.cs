using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Features.Chat;
using CarteraProyectos.Core.Interfaces;
using CarteraProyectos.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Shouldly;

namespace CarteraProyectos.UnitTests.Features.Chat;

public class SendChatMessageHandlerTests
{
    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static async Task<(AppDbContext db, Person person, Conversation conversation)> SetupAsync()
    {
        var db = CreateDb();
        var person = Person.CreateFromClaims("sub-1", "Usuario Test", "test@example.com");
        db.Persons.Add(person);
        await db.SaveChangesAsync();

        var conversation = Conversation.Create(person.Id, "Conversación de prueba");
        db.Conversations.Add(conversation);
        await db.SaveChangesAsync();

        return (db, person, conversation);
    }

    // ─── Turno sin tools (respuesta directa) ─────────────────────────────────

    [Fact]
    public async Task Handle_TurnWithoutTools_PersistsMessagesAndReturnsReply()
    {
        var (db, person, conversation) = await SetupAsync();

        var llm = Substitute.For<IChatCompletionClient>();
        llm.CompleteAsync(Arg.Any<ChatCompletionRequest>(), Arg.Any<CancellationToken>())
           .Returns(new ChatCompletionResponse(
               Role: "assistant",
               Content: "Hola, ¿en qué puedo ayudarte?",
               ToolCalls: null,
               FinishReason: "stop"));

        var sender = Substitute.For<ISender>();
        var handler = new SendChatMessageHandler(db, llm, sender);

        var result = await handler.Handle(
            new SendChatMessageCommand(person.Id, conversation.Id, "Hola"),
            CancellationToken.None);

        result.AssistantReply.ShouldBe("Hola, ¿en qué puedo ayudarte?");
        result.HitIterationLimit.ShouldBeFalse();

        // Debe haber persistido el mensaje de usuario y el del asistente
        var messages = await db.ChatMessages
            .Where(m => m.ConversationId == conversation.Id)
            .ToListAsync();
        messages.Count.ShouldBe(2);
        messages.Any(m => m.Role == ChatMessageRole.User).ShouldBeTrue();
        messages.Any(m => m.Role == ChatMessageRole.Assistant).ShouldBeTrue();

        // Solo se invocó el LLM una vez
        await llm.Received(1).CompleteAsync(
            Arg.Any<ChatCompletionRequest>(), Arg.Any<CancellationToken>());
    }

    // ─── Turno con una tool call ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_TurnWithOneTool_ExecutesToolAndContinues()
    {
        var (db, person, conversation) = await SetupAsync();

        var toolCallId = "call-abc123";

        var llm = Substitute.For<IChatCompletionClient>();

        // Primera llamada: el modelo solicita una tool
        llm.CompleteAsync(Arg.Any<ChatCompletionRequest>(), Arg.Any<CancellationToken>())
           .Returns(
               new ChatCompletionResponse(
                   Role: "assistant",
                   Content: null,
                   ToolCalls: new[]
                   {
                       new ChatToolCall(toolCallId, "get_my_tasks", "{}")
                   },
                   FinishReason: "tool_calls"),
               // Segunda llamada: respuesta final
               new ChatCompletionResponse(
                   Role: "assistant",
                   Content: "Tienes 3 tareas activas.",
                   ToolCalls: null,
                   FinishReason: "stop"));

        // La tool get_my_tasks envía AgentGetMyTasksQuery que retorna AgentMyTasksDto
        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<Core.Features.Agent.AgentGetMyTasksQuery>(), Arg.Any<CancellationToken>())
              .Returns(new Core.Features.Agent.AgentMyTasksDto(
                  "Usuario Test", "Desarrollador",
                  new List<Core.Features.Agent.AgentTaskSummaryDto>
                  {
                      new(1, "Tarea A", "InProgress", "High", "Usuario Test", null, null, null)
                  },
                  new List<Core.Features.Agent.AgentTaskSummaryDto>(),
                  0));

        var handler = new SendChatMessageHandler(db, llm, sender);
        var result = await handler.Handle(
            new SendChatMessageCommand(person.Id, conversation.Id, "¿Cuáles son mis tareas?"),
            CancellationToken.None);

        result.AssistantReply.ShouldBe("Tienes 3 tareas activas.");
        result.HitIterationLimit.ShouldBeFalse();

        // Debe haberse invocado el LLM dos veces (tool call + respuesta final)
        await llm.Received(2).CompleteAsync(
            Arg.Any<ChatCompletionRequest>(), Arg.Any<CancellationToken>());

        // Debe existir un mensaje Tool en BD
        var toolMessages = await db.ChatMessages
            .Where(m => m.ConversationId == conversation.Id && m.Role == ChatMessageRole.Tool)
            .ToListAsync();
        toolMessages.Count.ShouldBe(1);
        toolMessages[0].ToolName.ShouldBe("get_my_tasks");
    }

    // ─── Alcance del tope de iteraciones ─────────────────────────────────────

    [Fact]
    public async Task Handle_HitsIterationLimit_ReturnsWarningAndSetsFlag()
    {
        var (db, person, conversation) = await SetupAsync();

        // El LLM siempre responde con tool calls (bucle infinito simulado)
        var llm = Substitute.For<IChatCompletionClient>();
        llm.CompleteAsync(Arg.Any<ChatCompletionRequest>(), Arg.Any<CancellationToken>())
           .Returns(_ => new ChatCompletionResponse(
               Role: "assistant",
               Content: null,
               ToolCalls: new[]
               {
                   new ChatToolCall($"call-{Guid.NewGuid()}", "get_capacity", "{}")
               },
               FinishReason: "tool_calls"));

        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<Core.Features.Agent.AgentGetCapacityQuery>(), Arg.Any<CancellationToken>())
              .Returns(new Core.Features.Agent.AgentCapacityDto(
                  new List<Core.Features.Agent.AgentTeamCapacityDto>()));

        var handler = new SendChatMessageHandler(db, llm, sender);
        var result = await handler.Handle(
            new SendChatMessageCommand(person.Id, conversation.Id, "¿Cuál es la capacidad?"),
            CancellationToken.None);

        result.HitIterationLimit.ShouldBeTrue();
        result.AssistantReply.ShouldNotBeNullOrEmpty();

        // Se invocó exactamente MaxIterations veces (5)
        await llm.Received(5).CompleteAsync(
            Arg.Any<ChatCompletionRequest>(), Arg.Any<CancellationToken>());
    }

    // ─── Conversación de otro usuario → KeyNotFoundException ─────────────────

    [Fact]
    public async Task Handle_ToolThrowsUnauthorized_PersistsErrorAndContinues()
    {
        var (db, person, conversation) = await SetupAsync();

        var toolCallId = "call-denied";
        var llm = Substitute.For<IChatCompletionClient>();

        // Primera llamada: el modelo solicita create_person (requiere Gestor)
        // Segunda: respuesta final tras el error
        llm.CompleteAsync(Arg.Any<ChatCompletionRequest>(), Arg.Any<CancellationToken>())
           .Returns(
               new ChatCompletionResponse(
                   Role: "assistant",
                   Content: null,
                   ToolCalls: new[]
                   {
                       new ChatToolCall(toolCallId, "create_person",
                           """{"name":"Ana","email":"ana@test.com","role":"Desarrollador"}""")
                   },
                   FinishReason: "tool_calls"),
               new ChatCompletionResponse(
                   Role: "assistant",
                   Content: "No tienes permisos para crear personas.",
                   ToolCalls: null,
                   FinishReason: "stop"));

        // La tool intentará enviar AgentCreatePersonCommand, que lanzará UnauthorizedAccessException
        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<Core.Features.Agent.AgentCreatePersonCommand>(), Arg.Any<CancellationToken>())
              .Returns<Task<int>>(_ => throw new UnauthorizedAccessException("Solo el Gestor puede crear personas."));

        var handler = new SendChatMessageHandler(db, llm, sender);
        var result = await handler.Handle(
            new SendChatMessageCommand(person.Id, conversation.Id, "Crea una persona llamada Ana"),
            CancellationToken.None);

        result.AssistantReply.ShouldBe("No tienes permisos para crear personas.");
        result.HitIterationLimit.ShouldBeFalse();

        // El mensaje Tool debe contener el error serializado
        var toolMessages = await db.ChatMessages
            .Where(m => m.ConversationId == conversation.Id && m.Role == ChatMessageRole.Tool)
            .ToListAsync();
        toolMessages.Count.ShouldBe(1);
        toolMessages[0].Content.ShouldContain("error");
    }

    // ─── Conversación de otro usuario → KeyNotFoundException ─────────────────

    [Fact]
    public async Task Handle_ConversationOfAnotherUser_ThrowsUnauthorized()
    {
        var (db, _, _) = await SetupAsync();

        // Crear segundo usuario
        var other = Person.CreateFromClaims("sub-2", "Otro Usuario", "other@example.com");
        db.Persons.Add(other);
        await db.SaveChangesAsync();

        // Conversación del primer usuario
        var conv = db.Conversations.First();

        var llm = Substitute.For<IChatCompletionClient>();
        var sender = Substitute.For<ISender>();
        var handler = new SendChatMessageHandler(db, llm, sender);

        await Should.ThrowAsync<KeyNotFoundException>(async () =>
            await handler.Handle(
                new SendChatMessageCommand(other.Id, conv.Id, "Hola"),
                CancellationToken.None));

        // El LLM no debe haber sido invocado
        await llm.DidNotReceive().CompleteAsync(
            Arg.Any<ChatCompletionRequest>(), Arg.Any<CancellationToken>());
    }

    // ─── Conversación inexistente → KeyNotFoundException ─────────────────────

    [Fact]
    public async Task Handle_ConversationNotFound_ThrowsKeyNotFound()
    {
        var (db, person, _) = await SetupAsync();

        var llm = Substitute.For<IChatCompletionClient>();
        var sender = Substitute.For<ISender>();
        var handler = new SendChatMessageHandler(db, llm, sender);

        await Should.ThrowAsync<KeyNotFoundException>(async () =>
            await handler.Handle(
                new SendChatMessageCommand(person.Id, 99999, "Hola"),
                CancellationToken.None));
    }
}
