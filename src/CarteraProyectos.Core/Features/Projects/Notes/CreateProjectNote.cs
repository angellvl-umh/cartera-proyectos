using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Features.Projects;
using CarteraProyectos.Core.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.Projects.Notes;

public record CreateProjectNoteCommand(int ProjectId, int AuthorId, string Text) : IRequest<int>;

public sealed class CreateProjectNoteValidator : AbstractValidator<CreateProjectNoteCommand>
{
    public CreateProjectNoteValidator()
    {
        RuleFor(x => x.Text).NotEmpty().MaximumLength(4000);
    }
}

public sealed class CreateProjectNoteHandler(IAppDbContext db) : IRequestHandler<CreateProjectNoteCommand, int>
{
    public async Task<int> Handle(CreateProjectNoteCommand request, CancellationToken cancellationToken)
    {
        if (!await db.Projects.AnyAsync(p => p.Id == request.ProjectId, cancellationToken))
            throw new KeyNotFoundException($"Proyecto con Id {request.ProjectId} no encontrado.");

        var author = await db.Persons.FindAsync([request.AuthorId], cancellationToken)
            ?? throw new KeyNotFoundException($"Persona con Id {request.AuthorId} no encontrada.");

        // Gestor: siempre autorizado. Resto: debe pertenecer a un equipo asignado al proyecto.
        await ProjectAuthorization.EnsureCanManageProjectAsync(db, request.ProjectId, author, cancellationToken);

        var note = ProjectNote.Create(request.ProjectId, request.AuthorId, request.Text);
        db.ProjectNotes.Add(note);
        await db.SaveChangesAsync(cancellationToken);
        return note.Id;
    }
}
