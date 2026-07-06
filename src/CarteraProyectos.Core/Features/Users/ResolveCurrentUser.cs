using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.Users;

public enum ResolveUserStatus { Ok, NotRegistered, Inactive }

/// <summary>
/// Resuelve la identidad del usuario autenticado contra la tabla Persons.
/// Core no depende de IConfiguration: el endpoint pasa los emails de bootstrap.
/// </summary>
public record ResolveCurrentUserCommand(
    string SubjectId,
    string Name,
    string Email,
    string[] BootstrapGestorEmails) : IRequest<ResolveCurrentUserResult>;

public record ResolveCurrentUserResult(
    ResolveUserStatus Status,
    int? Id,
    string? SubjectId,
    string? Name,
    string? Email,
    bool? IsActive,
    string? Role);

public sealed class ResolveCurrentUserHandler(IAppDbContext db)
    : IRequestHandler<ResolveCurrentUserCommand, ResolveCurrentUserResult>
{
    public async Task<ResolveCurrentUserResult> Handle(
        ResolveCurrentUserCommand command, CancellationToken cancellationToken)
    {
        // 1. Buscar por SubjectId
        var person = await db.Persons
            .FirstOrDefaultAsync(p => p.SubjectId == command.SubjectId, cancellationToken);

        // 2. Si no existe por sub, buscar por email (pre-registro o realm recreado)
        if (person is null)
        {
            person = await db.Persons
                .FirstOrDefaultAsync(p => p.Email == command.Email, cancellationToken);

            if (person is not null)
            {
                // Vinculación: actualizar SubjectId
                person.UpdateSubjectId(command.SubjectId);
                await db.SaveChangesAsync(cancellationToken);
            }
        }

        // 3. Si sigue sin existir: bootstrap o no registrado
        if (person is null)
        {
            var isBootstrap = command.BootstrapGestorEmails
                .Contains(command.Email, StringComparer.OrdinalIgnoreCase);

            if (!isBootstrap)
                return new ResolveCurrentUserResult(
                    ResolveUserStatus.NotRegistered,
                    null, null, null, null, null, null);

            // Bootstrap: crear Gestor inicial
            person = Person.CreateFromClaims(
                command.SubjectId, command.Name, command.Email, PersonRole.Gestor);
            db.Persons.Add(person);
            await db.SaveChangesAsync(cancellationToken);
        }

        // 4. Persona inactiva
        if (!person.IsActive)
            return new ResolveCurrentUserResult(
                ResolveUserStatus.Inactive,
                null, null, null, null, null, null);

        // 5. Happy path
        return new ResolveCurrentUserResult(
            ResolveUserStatus.Ok,
            person.Id,
            person.SubjectId,
            person.Name,
            person.Email,
            person.IsActive,
            person.Role.ToString());
    }
}
