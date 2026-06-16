using CarteraProyectos.Core.Domain;
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

        // Gestor and JefeEquipo can add notes
        if (author.Role == PersonRole.Desarrollador)
        {
            var isInProjectTeam = await db.ProjectTeamAssignments
                .AnyAsync(a => a.ProjectId == request.ProjectId &&
                    db.PersonTeamMemberships.Any(m => m.PersonId == request.AuthorId && m.TeamId == a.TeamId),
                    cancellationToken);
            if (!isInProjectTeam)
                throw new UnauthorizedAccessException("Sin permisos para añadir notas a este proyecto.");
        }

        var note = ProjectNote.Create(request.ProjectId, request.AuthorId, request.Text);
        db.ProjectNotes.Add(note);
        await db.SaveChangesAsync(cancellationToken);
        return note.Id;
    }
}
