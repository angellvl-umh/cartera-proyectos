using CarteraProyectos.Core.Common;
using CarteraProyectos.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace CarteraProyectos.UnitTests.Common;

// ── Fixtures de prueba ────────────────────────────────────────────────────────

file record AuditableRequest(int RequestingPersonId, string Label)
    : IRequest<string>, IAgentAuditable;

file record NonAuditableRequest(string Label) : IRequest<string>;

file sealed class DummyHandler : IRequestHandler<AuditableRequest, string>,
                                  IRequestHandler<NonAuditableRequest, string>
{
    public Task<string> Handle(AuditableRequest request, CancellationToken ct) =>
        Task.FromResult("ok");

    public Task<string> Handle(NonAuditableRequest request, CancellationToken ct) =>
        Task.FromResult("ok");
}

file sealed class ThrowingHandler : IRequestHandler<AuditableRequest, string>
{
    public Task<string> Handle(AuditableRequest request, CancellationToken ct) =>
        throw new InvalidOperationException("boom");
}

// ── Tests ─────────────────────────────────────────────────────────────────────

public class AgentAuditBehaviorTests
{
    private static AppDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public async Task Handle_RequestImplementsIAgentAuditable_LogsActionAfterSuccess()
    {
        await using var db = CreateDb();
        var behavior = new AgentAuditBehavior<AuditableRequest, string>(db);
        var request = new AuditableRequest(42, "test-label");
        var handler = new DummyHandler();

        await behavior.Handle(request, (ct) => handler.Handle(request, ct), CancellationToken.None);

        var logs = await db.AgentActionLogs.ToListAsync();
        logs.Count.ShouldBe(1);
        logs[0].PersonId.ShouldBe(42);
        logs[0].ActionName.ShouldContain("AuditableRequest");
    }

    [Fact]
    public async Task Handle_RequestNotAuditable_DoesNotLog()
    {
        await using var db = CreateDb();
        var behavior = new AgentAuditBehavior<NonAuditableRequest, string>(db);
        var request = new NonAuditableRequest("label");
        var handler = new DummyHandler();

        await behavior.Handle(request, (ct) => handler.Handle(request, ct), CancellationToken.None);

        var count = await db.AgentActionLogs.CountAsync();
        count.ShouldBe(0);
    }

    [Fact]
    public async Task Handle_HandlerThrows_DoesNotLog()
    {
        await using var db = CreateDb();
        var behavior = new AgentAuditBehavior<AuditableRequest, string>(db);
        var request = new AuditableRequest(7, "boom");
        var handler = new ThrowingHandler();

        await Should.ThrowAsync<InvalidOperationException>(() =>
            behavior.Handle(request, (ct) => handler.Handle(request, ct), CancellationToken.None));

        var count = await db.AgentActionLogs.CountAsync();
        count.ShouldBe(0);
    }
}
