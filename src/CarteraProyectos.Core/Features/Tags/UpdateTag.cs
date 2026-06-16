using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.Tags;

public record UpdateTagCommand(int Id, string Name, string? Color, int RequestingPersonId = 0) : IRequest;

public sealed class UpdateTagValidator : AbstractValidator<UpdateTagCommand>
{
    public UpdateTagValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Color).MaximumLength(20).When(x => x.Color is not null);
    }
}

public sealed class UpdateTagHandler(IAppDbContext db) : IRequestHandler<UpdateTagCommand>
{
    public async Task Handle(UpdateTagCommand request, CancellationToken cancellationToken)
    {
        var requester = await db.Persons.FindAsync([request.RequestingPersonId], cancellationToken)
            ?? throw new KeyNotFoundException($"Persona con Id {request.RequestingPersonId} no encontrada.");
        if (requester.Role != PersonRole.Gestor)
            throw new UnauthorizedAccessException("Solo el Gestor puede gestionar etiquetas.");

        var tag = await db.Tags.FindAsync([request.Id], cancellationToken)
            ?? throw new KeyNotFoundException($"Etiqueta con Id {request.Id} no encontrada.");

        if (await db.Tags.AnyAsync(t => t.Name == request.Name && t.Id != request.Id, cancellationToken))
            throw new InvalidOperationException($"Ya existe una etiqueta con el nombre '{request.Name}'.");

        tag.Update(request.Name, request.Color);
        await db.SaveChangesAsync(cancellationToken);
    }
}
