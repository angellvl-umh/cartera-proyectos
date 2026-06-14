using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.Comments;

public record CreateCommentCommand(
    int ProjectId,
    int WorkItemId,
    int AuthorId,
    string Text) : IRequest<int>;

public sealed class CreateCommentValidator : AbstractValidator<CreateCommentCommand>
{
    public CreateCommentValidator()
    {
        RuleFor(x => x.Text).NotEmpty().MaximumLength(2000);
    }
}

public sealed class CreateCommentHandler(IAppDbContext db) : IRequestHandler<CreateCommentCommand, int>
{
    public async Task<int> Handle(CreateCommentCommand request, CancellationToken ct)
    {
        var workItem = await db.WorkItems.FirstOrDefaultAsync(
            w => w.Id == request.WorkItemId && w.ProjectId == request.ProjectId, ct);
        if (workItem is null) throw new KeyNotFoundException($"WorkItem {request.WorkItemId} no encontrado.");

        var comment = Comment.Create(request.WorkItemId, request.AuthorId, request.Text);
        db.Comments.Add(comment);
        await db.SaveChangesAsync(ct);
        return comment.Id;
    }
}
