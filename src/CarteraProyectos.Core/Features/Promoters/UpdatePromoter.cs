using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.Promoters;

public record UpdatePromoterCommand(int Id, string Name, int RequestingPersonId = 0) : IRequest;

public sealed class UpdatePromoterValidator : AbstractValidator<UpdatePromoterCommand>
{
    public UpdatePromoterValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}

public sealed class UpdatePromoterHandler(IAppDbContext db) : IRequestHandler<UpdatePromoterCommand>
{
    public async Task Handle(UpdatePromoterCommand request, CancellationToken cancellationToken)
    {
        var requester = await db.Persons.FindAsync([request.RequestingPersonId], cancellationToken)
            ?? throw new KeyNotFoundException($"Persona con Id {request.RequestingPersonId} no encontrada.");
        if (requester.Role != PersonRole.Gestor)
            throw new UnauthorizedAccessException("Solo el Gestor puede gestionar promotores.");

        var promoter = await db.Promoters.FindAsync([request.Id], cancellationToken)
            ?? throw new KeyNotFoundException($"Promotor con Id {request.Id} no encontrado.");

        if (await db.Promoters.AnyAsync(p => p.Name == request.Name && p.Id != request.Id, cancellationToken))
            throw new InvalidOperationException($"Ya existe un promotor con el nombre '{request.Name}'.");

        promoter.Update(request.Name);
        await db.SaveChangesAsync(cancellationToken);
    }
}
