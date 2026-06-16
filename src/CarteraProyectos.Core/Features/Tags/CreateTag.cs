using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.Tags;

public record CreateTagCommand(string Name, string? Color, int RequestingPersonId = 0) : IRequest<int>;

public sealed class CreateTagValidator : AbstractValidator<CreateTagCommand>
{
    public CreateTagValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Color).MaximumLength(20).When(x => x.Color is not null);
    }
}

public sealed class CreateTagHandler(IAppDbContext db) : IRequestHandler<CreateTagCommand, int>
{
    public async Task<int> Handle(CreateTagCommand request, CancellationToken cancellationToken)
    {
        var requester = await db.Persons.FindAsync([request.RequestingPersonId], cancellationToken)
            ?? throw new KeyNotFoundException($"Persona con Id {request.RequestingPersonId} no encontrada.");
        if (requester.Role != PersonRole.Gestor)
            throw new UnauthorizedAccessException("Solo el Gestor puede gestionar etiquetas.");

        if (await db.Tags.AnyAsync(t => t.Name == request.Name, cancellationToken))
            throw new InvalidOperationException($"Ya existe una etiqueta con el nombre '{request.Name}'.");

        var tag = Tag.Create(request.Name, request.Color);
        db.Tags.Add(tag);
        await db.SaveChangesAsync(cancellationToken);
        return tag.Id;
    }
}
