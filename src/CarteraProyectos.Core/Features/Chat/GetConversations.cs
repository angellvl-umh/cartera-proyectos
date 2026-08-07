using CarteraProyectos.Core.Common;
using CarteraProyectos.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.Chat;

public record ConversationSummaryDto(
    int Id,
    string Title,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int MessageCount);

public record GetConversationsQuery(int PersonId, int Page = 1, int PageSize = 20)
    : IRequest<PagedResult<ConversationSummaryDto>>;

public sealed class GetConversationsHandler(IAppDbContext db)
    : IRequestHandler<GetConversationsQuery, PagedResult<ConversationSummaryDto>>
{
    public async Task<PagedResult<ConversationSummaryDto>> Handle(
        GetConversationsQuery request, CancellationToken ct)
    {
        var query = db.Conversations
            .Where(c => c.PersonId == request.PersonId)
            .OrderByDescending(c => c.UpdatedAt);

        var total = await query.CountAsync(ct);

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new ConversationSummaryDto(
                c.Id,
                c.Title,
                c.CreatedAt,
                c.UpdatedAt,
                c.Messages.Count))
            .ToListAsync(ct);

        return new PagedResult<ConversationSummaryDto>(items, total, request.Page, request.PageSize);
    }
}
