using CarteraProyectos.Core.Common;
using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Features.Tags;
using CarteraProyectos.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.Projects;

public record GetProjectsQuery(
    ProjectStatus? Status = null,
    int? PortfolioYear = null,
    int? TeamId = null,
    ProjectComplexity? Complexity = null,
    string? Q = null,
    int? TagId = null,
    int[]? TagIds = null,
    SiptGroup? SiptGroup = null,
    int? PromoterId = null,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<ProjectListDto>>;

public record ProjectListDto(
    int Id,
    string Title,
    string? RequestingUnit,
    string Complexity,
    string Status,
    int? PortfolioYear,
    DateOnly? StartDate,
    DateOnly? EndDate,
    int? GroupPriority,
    string? SiptGroup,
    int? PromoterId,
    string? PromoterName,
    List<TagDto> Tags);

public sealed class GetProjectsHandler(IAppDbContext db) : IRequestHandler<GetProjectsQuery, PagedResult<ProjectListDto>>
{
    public async Task<PagedResult<ProjectListDto>> Handle(GetProjectsQuery request, CancellationToken cancellationToken)
    {
        var pageSize = Math.Min(request.PageSize, 100);
        var page = Math.Max(request.Page, 1);

        var query = db.Projects
            .Include(p => p.Promoter)
            .Include(p => p.Tags)
            .AsQueryable();

        if (request.Status.HasValue)
            query = query.Where(p => p.Status == request.Status.Value);

        if (request.PortfolioYear.HasValue)
            query = query.Where(p => p.PortfolioYear == request.PortfolioYear.Value);

        if (request.TeamId.HasValue)
            query = query.Where(p => p.Teams.Any(t => t.TeamId == request.TeamId.Value));

        if (request.Complexity.HasValue)
            query = query.Where(p => p.Complexity == request.Complexity.Value);

        if (!string.IsNullOrWhiteSpace(request.Q))
        {
            var q = request.Q.ToLower();
            query = query.Where(p => p.Title.ToLower().Contains(q)
                                  || (p.Description != null && p.Description.ToLower().Contains(q)));
        }

        if (request.TagIds is { Length: > 0 })
            query = query.Where(p => p.Tags.Any(t => request.TagIds.Contains(t.Id)));
        else if (request.TagId.HasValue)
            query = query.Where(p => p.Tags.Any(t => t.Id == request.TagId.Value));

        if (request.SiptGroup.HasValue)
            query = query.Where(p => p.SiptGroup == request.SiptGroup.Value);

        if (request.PromoterId.HasValue)
            query = query.Where(p => p.PromoterId == request.PromoterId.Value);

        var ordered = query.OrderByDescending(p => p.Id);
        var total = await ordered.CountAsync(cancellationToken);

        var items = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ProjectListDto>(
            items.Select(p => new ProjectListDto(
                p.Id, p.Title, p.RequestingUnit,
                p.Complexity.ToString(), p.Status.ToString(),
                p.PortfolioYear, p.StartDate, p.EndDate,
                p.GroupPriority, p.SiptGroup?.ToString(),
                p.PromoterId, p.Promoter?.Name,
                p.Tags.Select(t => new TagDto(t.Id, t.Name, t.Color)).ToList())).ToList(),
            total, page, pageSize);
    }
}
