using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.Epics;

public interface IEpicService
{
    Task<int> CreateAsync(CreateEpicCommand command, CancellationToken ct);
    Task UpdateAsync(UpdateEpicCommand command, CancellationToken ct);
}

public sealed class EpicService(IAppDbContext db) : IEpicService
{
    public async Task<int> CreateAsync(CreateEpicCommand request, CancellationToken ct)
    {
        var projectExists = await db.Projects.AnyAsync(p => p.Id == request.ProjectId, ct);
        if (!projectExists) throw new KeyNotFoundException($"Proyecto {request.ProjectId} no encontrado.");

        var epic = Epic.Create(request.ProjectId, request.Title, request.Description,
            request.Priority, request.SortOrder, request.EstimationHours, request.EstimationPoints);
        db.Epics.Add(epic);
        await db.SaveChangesAsync(ct);
        return epic.Id;
    }

    public async Task UpdateAsync(UpdateEpicCommand request, CancellationToken ct)
    {
        var epic = await db.Epics.FirstOrDefaultAsync(e => e.Id == request.Id, ct)
            ?? throw new KeyNotFoundException($"Épica {request.Id} no encontrada.");

        epic.Update(request.Title, request.Description, request.Priority, request.SortOrder,
            request.EstimationHours, request.EstimationPoints);
        await db.SaveChangesAsync(ct);
    }
}
