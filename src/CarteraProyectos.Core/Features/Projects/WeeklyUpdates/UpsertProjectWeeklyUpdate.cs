using CarteraProyectos.Core.Common;
using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Features.Projects;
using CarteraProyectos.Core.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.Projects.WeeklyUpdates;

public record UpsertProjectWeeklyUpdateCommand(int ProjectId, int AuthorId, string Summary, ProjectHealthStatus HealthStatus) : IRequest<int>, IAgentAuditable
{
    public int RequestingPersonId => AuthorId;
}

public sealed class UpsertProjectWeeklyUpdateValidator : AbstractValidator<UpsertProjectWeeklyUpdateCommand>
{
    public UpsertProjectWeeklyUpdateValidator()
    {
        RuleFor(x => x.Summary).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.HealthStatus).IsInEnum();
    }
}

public sealed class UpsertProjectWeeklyUpdateHandler(IAppDbContext db) : IRequestHandler<UpsertProjectWeeklyUpdateCommand, int>
{
    public async Task<int> Handle(UpsertProjectWeeklyUpdateCommand request, CancellationToken cancellationToken)
    {
        _ = await db.Projects
            .FirstOrDefaultAsync(p => p.Id == request.ProjectId, cancellationToken)
            ?? throw new KeyNotFoundException($"Proyecto {request.ProjectId} no encontrado.");

        var author = await db.Persons.FindAsync([request.AuthorId], cancellationToken)
            ?? throw new KeyNotFoundException($"Persona con Id {request.AuthorId} no encontrada.");

        // Gestor: siempre autorizado. Resto: debe pertenecer a un equipo asignado al proyecto.
        await ProjectAuthorization.EnsureCanManageProjectAsync(db, request.ProjectId, author, cancellationToken);

        // Calcular WeekOf: lunes de la semana ISO actual
        var now = DateTime.UtcNow;
        var weekOf = GetMondayOfWeek(now);

        // Buscar o crear ProjectWeeklyUpdate
        var existing = await db.ProjectWeeklyUpdates
            .FirstOrDefaultAsync(
                u => u.ProjectId == request.ProjectId && u.AuthorId == request.AuthorId && u.WeekOf == weekOf,
                cancellationToken);

        if (existing is not null)
        {
            existing.Update(request.Summary, request.HealthStatus);
        }
        else
        {
            var update = ProjectWeeklyUpdate.Create(
                request.ProjectId, request.AuthorId, weekOf, request.Summary, request.HealthStatus);
            db.ProjectWeeklyUpdates.Add(update);
        }

        await db.SaveChangesAsync(cancellationToken);

        // Retornar el ID (del registro existente o del nuevo)
        var result = await db.ProjectWeeklyUpdates
            .Where(u => u.ProjectId == request.ProjectId && u.AuthorId == request.AuthorId && u.WeekOf == weekOf)
            .Select(u => u.Id)
            .FirstAsync(cancellationToken);

        return result;
    }

    private static DateOnly GetMondayOfWeek(DateTime date)
    {
        var dateOnly = DateOnly.FromDateTime(date);
        var daysOfWeek = (int)dateOnly.DayOfWeek;
        var daysToMonday = daysOfWeek == 0 ? 6 : daysOfWeek - 1;
        return dateOnly.AddDays(-daysToMonday);
    }
}
