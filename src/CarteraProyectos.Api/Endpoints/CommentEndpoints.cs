using CarteraProyectos.Core.Features.Comments;
using CarteraProyectos.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Api.Endpoints;

public static class CommentEndpoints
{
    public static void MapCommentEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/projects/{projectId:int}/workitems/{workItemId:int}/comments")
            .WithTags("Comments")
            .RequireAuthorization();

        group.MapGet("/", GetComments)
            .WithName("GetComments")
            .WithDescription("Lista los comentarios de una tarea ordenados por fecha de creación.");

        group.MapPost("/", CreateComment)
            .WithName("CreateComment")
            .WithDescription("Crea un comentario en una tarea.");

        group.MapDelete("/{commentId:int}", DeleteComment)
            .WithName("DeleteComment")
            .WithDescription("Elimina un comentario. Solo el autor puede eliminarlo.");
    }

    private static async Task<IResult> GetComments(
        int projectId, int workItemId, IMediator mediator, CancellationToken ct)
        => Results.Ok(await mediator.Send(new GetCommentsQuery(projectId, workItemId), ct));

    private static async Task<IResult> CreateComment(
        int projectId, int workItemId, CreateCommentRequest req,
        HttpContext ctx, IAppDbContext db, IMediator mediator, CancellationToken ct)
    {
        var sub = ctx.User.FindFirst("sub")?.Value;
        var person = sub is not null
            ? await db.Persons.FirstOrDefaultAsync(p => p.SubjectId == sub, ct)
            : null;
        if (person is null) return Results.Unauthorized();

        var id = await mediator.Send(
            new CreateCommentCommand(projectId, workItemId, person.Id, req.Text), ct);
        return Results.Created($"/api/projects/{projectId}/workitems/{workItemId}/comments/{id}", new { id });
    }

    private static async Task<IResult> DeleteComment(
        int projectId, int workItemId, int commentId,
        HttpContext ctx, IAppDbContext db, IMediator mediator, CancellationToken ct)
    {
        var sub = ctx.User.FindFirst("sub")?.Value;
        var person = sub is not null
            ? await db.Persons.FirstOrDefaultAsync(p => p.SubjectId == sub, ct)
            : null;
        if (person is null) return Results.Unauthorized();

        await mediator.Send(new DeleteCommentCommand(commentId, person.Id), ct);
        return Results.NoContent();
    }
}

record CreateCommentRequest(string Text);
