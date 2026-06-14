using CarteraProyectos.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.Comments;

public record GetCommentsQuery(int ProjectId, int WorkItemId) : IRequest<List<CommentDto>>;

public record CommentDto(
    int Id,
    int WorkItemId,
    int AuthorId,
    string AuthorName,
    string Text,
    DateTime CreatedAt);

public sealed class GetCommentsHandler(IAppDbContext db) : IRequestHandler<GetCommentsQuery, List<CommentDto>>
{
    public async Task<List<CommentDto>> Handle(GetCommentsQuery request, CancellationToken ct)
    {
        var exists = await db.WorkItems.AnyAsync(
            w => w.Id == request.WorkItemId && w.ProjectId == request.ProjectId, ct);
        if (!exists) throw new KeyNotFoundException($"WorkItem {request.WorkItemId} no encontrado.");

        return await db.Comments
            .Where(c => c.WorkItemId == request.WorkItemId)
            .Include(c => c.Author)
            .OrderBy(c => c.CreatedAt)
            .Select(c => new CommentDto(c.Id, c.WorkItemId, c.AuthorId, c.Author!.Name, c.Text, c.CreatedAt))
            .ToListAsync(ct);
    }
}
