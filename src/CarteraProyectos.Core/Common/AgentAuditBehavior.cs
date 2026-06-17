using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Interfaces;
using MediatR;

namespace CarteraProyectos.Core.Common;

public sealed class AgentAuditBehavior<TRequest, TResponse>(IAppDbContext db)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (request is not IAgentAuditable auditable)
            return await next(ct);

        var response = await next(ct);

        var raw = request.ToString() ?? string.Empty;
        var payload = raw.Length > 500 ? raw[..500] : raw;

        db.AgentActionLogs.Add(AgentActionLog.Create(
            auditable.RequestingPersonId,
            typeof(TRequest).Name,
            payload));

        await db.SaveChangesAsync(ct);

        return response;
    }
}
