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

public sealed class AgentCreateSprintHandler(ISprintLifecycleService service)
    : IRequestHandler<AgentCreateSprintCommand, int>
{
    public Task<int> Handle(AgentCreateSprintCommand request, CancellationToken ct)
        => service.CreateAsync(
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

public sealed class AgentActivateSprintHandler(ISprintLifecycleService service)
    : IRequestHandler<AgentActivateSprintCommand>
{
    public Task Handle(AgentActivateSprintCommand request, CancellationToken ct)
        => service.TransitionStatusAsync(
            new TransitionSprintStatusCommand(
                request.SprintId,
                SprintStatus.Active,
                request.PersonId),
            ct);
}

public sealed class AgentCompleteSprintHandler(ISprintLifecycleService service)
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

        await service.TransitionStatusAsync(
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

public sealed class AgentCreateEpicHandler(IEpicService service)
    : IRequestHandler<AgentCreateEpicCommand, int>
{
    public Task<int> Handle(AgentCreateEpicCommand request, CancellationToken ct)
        => service.CreateAsync(
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

public sealed class AgentUpdateEpicHandler(IEpicService service)
    : IRequestHandler<AgentUpdateEpicCommand>
{
    public Task Handle(AgentUpdateEpicCommand request, CancellationToken ct)
        => service.UpdateAsync(
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
