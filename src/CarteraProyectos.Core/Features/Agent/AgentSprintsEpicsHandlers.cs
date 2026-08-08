using CarteraProyectos.Core.Common;
using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Features.Epics;
using CarteraProyectos.Core.Features.Sprints;
using MediatR;

namespace CarteraProyectos.Core.Features.Agent;

// ─── Sprint Commands ──────────────────────────────────────────────────────────

public record AgentCreateSprintCommand(
    int PersonId,
    int ProjectId,
    string Name,
    string? Goal,
    DateOnly? StartDate,
    DateOnly? EndDate,
    int? Capacity) : IRequest<int>, IAgentAuditable
{
    public int RequestingPersonId => PersonId;
}

public record AgentActivateSprintCommand(
    int PersonId,
    int SprintId) : IRequest, IAgentAuditable
{
    public int RequestingPersonId => PersonId;
}

public record AgentCompleteSprintCommand(
    int PersonId,
    int SprintId,
    string? CarryOver,
    int? TargetSprintId) : IRequest, IAgentAuditable
{
    public int RequestingPersonId => PersonId;
}

// ─── Epic Commands ────────────────────────────────────────────────────────────

public record AgentCreateEpicCommand(
    int PersonId,
    int ProjectId,
    string Title,
    string? Description,
    int Priority,
    int SortOrder,
    int? EstimationHours,
    int? EstimationPoints) : IRequest<int>, IAgentAuditable
{
    public int RequestingPersonId => PersonId;
}

public record AgentUpdateEpicCommand(
    int PersonId,
    int EpicId,
    string Title,
    string? Description,
    int Priority,
    int SortOrder,
    int? EstimationHours,
    int? EstimationPoints) : IRequest, IAgentAuditable
{
    public int RequestingPersonId => PersonId;
}

// ─── Sprint Handlers ──────────────────────────────────────────────────────────

public sealed class AgentCreateSprintHandler(ISender sender)
    : IRequestHandler<AgentCreateSprintCommand, int>
{
    public async Task<int> Handle(AgentCreateSprintCommand request, CancellationToken ct)
        => await sender.Send(
            new CreateSprintCommand(
                request.ProjectId,
                request.Name,
                request.Goal,
                request.StartDate,
                request.EndDate,
                request.Capacity,
                request.PersonId),
            ct);
}

public sealed class AgentActivateSprintHandler(ISender sender)
    : IRequestHandler<AgentActivateSprintCommand>
{
    public async Task Handle(AgentActivateSprintCommand request, CancellationToken ct)
        => await sender.Send(
            new TransitionSprintStatusCommand(
                request.SprintId,
                SprintStatus.Active,
                request.PersonId),
            ct);
}

public sealed class AgentCompleteSprintHandler(ISender sender)
    : IRequestHandler<AgentCompleteSprintCommand>
{
    public async Task Handle(AgentCompleteSprintCommand request, CancellationToken ct)
    {
        CarryOverTarget? carryOver = null;
        if (request.CarryOver is not null)
        {
            if (!Enum.TryParse<CarryOverTarget>(request.CarryOver, out var parsed))
                throw new InvalidOperationException(
                    "CarryOver no válido. Valores aceptados: Backlog, Sprint.");
            carryOver = parsed;
        }

        await sender.Send(
            new TransitionSprintStatusCommand(
                request.SprintId,
                SprintStatus.Completed,
                request.PersonId,
                carryOver,
                request.TargetSprintId),
            ct);
    }
}

// ─── Epic Handlers ────────────────────────────────────────────────────────────

public sealed class AgentCreateEpicHandler(ISender sender)
    : IRequestHandler<AgentCreateEpicCommand, int>
{
    public async Task<int> Handle(AgentCreateEpicCommand request, CancellationToken ct)
        => await sender.Send(
            new CreateEpicCommand(
                request.ProjectId,
                request.Title,
                request.Description,
                request.Priority,
                request.SortOrder,
                request.EstimationHours,
                request.EstimationPoints),
            ct);
}

public sealed class AgentUpdateEpicHandler(ISender sender)
    : IRequestHandler<AgentUpdateEpicCommand>
{
    public async Task Handle(AgentUpdateEpicCommand request, CancellationToken ct)
        => await sender.Send(
            new UpdateEpicCommand(
                request.EpicId,
                request.Title,
                request.Description,
                request.Priority,
                request.SortOrder,
                request.EstimationHours,
                request.EstimationPoints),
            ct);
}
