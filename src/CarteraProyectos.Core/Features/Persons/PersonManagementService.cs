using CarteraProyectos.Core.Common;
using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.Persons;

public interface IPersonManagementService
{
    Task<CreatePersonResult> CreateAsync(CreatePersonCommand command, CancellationToken ct);
    Task<PagedResult<PersonListDto>> GetListAsync(GetPersonsQuery query, CancellationToken ct);
    Task UpdateAsync(UpdatePersonCommand command, CancellationToken ct);
    Task SetActiveAsync(SetPersonActiveCommand command, CancellationToken ct);
}

public sealed class PersonManagementService(IAppDbContext db, IIdentityProviderService idp) : IPersonManagementService
{
    public async Task<CreatePersonResult> CreateAsync(CreatePersonCommand request, CancellationToken ct)
    {
        var requester = await db.Persons.FindAsync([request.RequestingPersonId], ct)
            ?? throw new KeyNotFoundException($"Persona con Id {request.RequestingPersonId} no encontrada.");

        if (requester.Role != PersonRole.Gestor)
            throw new UnauthorizedAccessException("Solo el Gestor puede crear personas.");

        var exists = await db.Persons.AnyAsync(
            p => p.Email.ToLower() == request.Email.ToLower(),
            ct);

        if (exists)
            throw new InvalidOperationException("Ya existe una persona con ese email.");

        var person = Person.Create(request.Name, request.Email, request.Role);
        db.Persons.Add(person);
        await db.SaveChangesAsync(ct);

        if (!request.CreateLocalCredentials)
            return new CreatePersonResult(person.Id, null, null);

        var idpResult = await idp.CreateUserWithTemporaryPasswordAsync(request.Name, request.Email, ct);

        return idpResult.Status switch
        {
            IdentityCredentialsStatus.Created =>
                new CreatePersonResult(person.Id, idpResult.TemporaryPassword, null),

            IdentityCredentialsStatus.AlreadyExists =>
                new CreatePersonResult(
                    person.Id,
                    null,
                    "Ya existía una cuenta en el proveedor de identidad para ese email; no se ha creado una nueva."),

            _ => // Unavailable
                new CreatePersonResult(
                    person.Id,
                    null,
                    "La persona se ha creado, pero no se pudo crear la cuenta local en el proveedor de identidad. " +
                    "Puede crearse más tarde o el usuario puede entrar con Google/SSO."),
        };
    }


    public async Task<PagedResult<PersonListDto>> GetListAsync(GetPersonsQuery request, CancellationToken ct)
    {
        var pageSize = Math.Min(request.PageSize, 100);
        var page = Math.Max(request.Page, 1);

        var baseQuery = db.Persons
            .Where(p => request.IncludeInactive || p.IsActive)
            .OrderBy(p => p.Name);

        var total = await baseQuery.CountAsync(ct);

        var items = await baseQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PersonListDto(
                p.Id, p.Name, p.Email, p.Role.ToString(), p.IsActive, p.SubjectId != null))
            .ToListAsync(ct);

        return new PagedResult<PersonListDto>(items, total, page, pageSize);
    }

    public async Task UpdateAsync(UpdatePersonCommand request, CancellationToken ct)
    {
        var requester = await db.Persons.FindAsync([request.RequestingPersonId], ct)
            ?? throw new KeyNotFoundException($"Persona con Id {request.RequestingPersonId} no encontrada.");

        if (requester.Role != PersonRole.Gestor)
            throw new UnauthorizedAccessException("Solo el Gestor puede actualizar personas.");

        var person = await db.Persons.FindAsync([request.PersonId], ct)
            ?? throw new KeyNotFoundException($"Persona con Id {request.PersonId} no encontrada.");

        // Verificar email duplicado (de otra persona)
        var emailExists = await db.Persons.AnyAsync(
            p => p.Id != request.PersonId && p.Email.ToLower() == request.Email.ToLower(),
            ct);

        if (emailExists)
            throw new InvalidOperationException("Ya existe otra persona con ese email.");

        person.Update(request.Name, request.Email);
        person.UpdateRole(request.Role);

        await db.SaveChangesAsync(ct);
    }

    public async Task SetActiveAsync(SetPersonActiveCommand request, CancellationToken ct)
    {
        var requester = await db.Persons.FindAsync([request.RequestingPersonId], ct)
            ?? throw new KeyNotFoundException($"Persona con Id {request.RequestingPersonId} no encontrada.");

        if (requester.Role != PersonRole.Gestor)
            throw new UnauthorizedAccessException("Solo el Gestor puede cambiar el estado de las personas.");

        var person = await db.Persons.FindAsync([request.PersonId], ct)
            ?? throw new KeyNotFoundException($"Persona con Id {request.PersonId} no encontrada.");

        if (!request.IsActive && request.PersonId == request.RequestingPersonId)
            throw new InvalidOperationException("No puedes desactivarte a ti mismo.");

        if (request.IsActive)
            person.Activate();
        else
            person.Deactivate();

        await db.SaveChangesAsync(ct);
    }
}
