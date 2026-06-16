using CarteraProyectos.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.Tags;

public record GetTagsQuery : IRequest<List<TagDto>>;

public record TagDto(int Id, string Name, string? Color);

public sealed class GetTagsHandler(IAppDbContext db) : IRequestHandler<GetTagsQuery, List<TagDto>>
{
    public async Task<List<TagDto>> Handle(GetTagsQuery request, CancellationToken cancellationToken)
    {
        return await db.Tags
            .OrderBy(t => t.Name)
            .Select(t => new TagDto(t.Id, t.Name, t.Color))
            .ToListAsync(cancellationToken);
    }
}
