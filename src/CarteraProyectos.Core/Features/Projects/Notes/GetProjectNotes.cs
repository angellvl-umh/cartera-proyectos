using CarteraProyectos.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.Projects.Notes;

public record GetProjectNotesQuery(int ProjectId) : IRequest<List<ProjectNoteDto>>;

public record ProjectNoteDto(int Id, int ProjectId, int AuthorId, string AuthorName, string Text, DateTimeOffset CreatedAt);

public sealed class GetProjectNotesHandler(IAppDbContext db) : IRequestHandler<GetProjectNotesQuery, List<ProjectNoteDto>>
{
    public async Task<List<ProjectNoteDto>> Handle(GetProjectNotesQuery request, CancellationToken cancellationToken)
    {
        if (!await db.Projects.AnyAsync(p => p.Id == request.ProjectId, cancellationToken))
            throw new KeyNotFoundException($"Proyecto con Id {request.ProjectId} no encontrado.");

        return await db.ProjectNotes
            .Include(n => n.Author)
            .Where(n => n.ProjectId == request.ProjectId)
            .OrderBy(n => n.CreatedAt)
            .Select(n => new ProjectNoteDto(
                n.Id, n.ProjectId, n.AuthorId,
                n.Author!.Name, n.Text, n.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}
