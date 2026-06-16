using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.Promoters;

public record CreatePromoterCommand(string Name, int RequestingPersonId = 0) : IRequest<int>;

public sealed class CreatePromoterValidator : AbstractValidator<CreatePromoterCommand>
{
    public CreatePromoterValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}

public sealed class CreatePromoterHandler(IAppDbContext db) : IRequestHandler<CreatePromoterCommand, int>
{
    public async Task<int> Handle(CreatePromoterCommand request, CancellationToken cancellationToken)
    {
        var requester = await db.Persons.FindAsync([request.RequestingPersonId], cancellationToken)
            ?? throw new KeyNotFoundException($"Persona con Id {request.RequestingPersonId} no encontrada.");
        if (requester.Role != PersonRole.Gestor)
            throw new UnauthorizedAccessException("Solo el Gestor puede gestionar promotores.");

        if (await db.Promoters.AnyAsync(p => p.Name == request.Name, cancellationToken))
            throw new InvalidOperationException($"Ya existe un promotor con el nombre '{request.Name}'.");

        var promoter = Promoter.Create(request.Name);
        db.Promoters.Add(promoter);
        await db.SaveChangesAsync(cancellationToken);
        return promoter.Id;
    }
}
