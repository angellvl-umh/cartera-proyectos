using CarteraProyectos.Core.Common;
using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Features.Persons;
using MediatR;

namespace CarteraProyectos.Core.Features.Agent;

// ─── DTOs ────────────────────────────────────────────────────────────────────

// PersonListDto reused from Persons/GetPersons.cs

// ─── Queries / Commands ───────────────────────────────────────────────────────

public record AgentGetPersonsQuery(bool IncludeInactive = false) : IRequest<IReadOnlyList<PersonListDto>>;

public record AgentCreatePersonCommand(
    int PersonId, string Name, string Email, string Role) : IRequest<int>, IAgentAuditable
{
    public int RequestingPersonId => PersonId;
}

public record AgentUpdatePersonCommand(
    int PersonId, int TargetPersonId, string Name, string Email, string Role) : IRequest, IAgentAuditable
{
    public int RequestingPersonId => PersonId;
}

public record AgentSetPersonActiveCommand(
    int PersonId, int TargetPersonId, bool IsActive) : IRequest, IAgentAuditable
{
    public int RequestingPersonId => PersonId;
}

// ─── Handlers ────────────────────────────────────────────────────────────────

public sealed class AgentGetPersonsHandler(ISender sender)
    : IRequestHandler<AgentGetPersonsQuery, IReadOnlyList<PersonListDto>>
{
    public async Task<IReadOnlyList<PersonListDto>> Handle(AgentGetPersonsQuery request, CancellationToken ct)
    {
        var result = await sender.Send(new GetPersonsQuery(1, 100, request.IncludeInactive), ct);
        return result.Items;
    }
}

public sealed class AgentCreatePersonHandler(ISender sender)
    : IRequestHandler<AgentCreatePersonCommand, int>
{
    public async Task<int> Handle(AgentCreatePersonCommand request, CancellationToken ct)
    {
        if (!Enum.TryParse<PersonRole>(request.Role, out var role))
            throw new InvalidOperationException("Rol no válido. Valores aceptados: Desarrollador, Gestor.");
        
        if (role == PersonRole.JefeEquipo)
            throw new InvalidOperationException("Rol no válido. Valores aceptados: Desarrollador, Gestor.");

        return await sender.Send(new CreatePersonCommand(request.Name, request.Email, role, request.PersonId), ct);
    }
}

public sealed class AgentUpdatePersonHandler(ISender sender)
    : IRequestHandler<AgentUpdatePersonCommand>
{
    public async Task Handle(AgentUpdatePersonCommand request, CancellationToken ct)
    {
        if (!Enum.TryParse<PersonRole>(request.Role, out var role))
            throw new InvalidOperationException("Rol no válido. Valores aceptados: Desarrollador, Gestor.");
        
        if (role == PersonRole.JefeEquipo)
            throw new InvalidOperationException("Rol no válido. Valores aceptados: Desarrollador, Gestor.");

        await sender.Send(new UpdatePersonCommand(request.TargetPersonId, request.Name, request.Email, role, request.PersonId), ct);
    }
}

public sealed class AgentSetPersonActiveHandler(ISender sender)
    : IRequestHandler<AgentSetPersonActiveCommand>
{
    public async Task Handle(AgentSetPersonActiveCommand request, CancellationToken ct)
    {
        await sender.Send(new SetPersonActiveCommand(request.TargetPersonId, request.IsActive, request.PersonId), ct);
    }
}
