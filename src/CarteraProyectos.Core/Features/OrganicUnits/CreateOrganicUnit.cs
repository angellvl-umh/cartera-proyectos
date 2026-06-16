using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.OrganicUnits;

public record CreateOrganicUnitCommand(string Name, string? Code, int RequestingPersonId = 0) : IRequest<int>;

public sealed class CreateOrganicUnitValidator : AbstractValidator<CreateOrganicUnitCommand>
{
    public CreateOrganicUnitValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Code).MaximumLength(50).When(x => x.Code is not null);
    }
}

public sealed class CreateOrganicUnitHandler(IAppDbContext db) : IRequestHandler<CreateOrganicUnitCommand, int>
{
    public async Task<int> Handle(CreateOrganicUnitCommand request, CancellationToken cancellationToken)
    {
        var requester = await db.Persons.FindAsync([request.RequestingPersonId], cancellationToken)
            ?? throw new KeyNotFoundException($"Persona con Id {request.RequestingPersonId} no encontrada.");
        if (requester.Role != PersonRole.Gestor)
            throw new UnauthorizedAccessException("Solo el Gestor puede gestionar unidades orgánicas.");

        if (await db.OrganicUnits.AnyAsync(u => u.Name == request.Name, cancellationToken))
            throw new InvalidOperationException($"Ya existe una unidad orgánica con el nombre '{request.Name}'.");

        var unit = OrganicUnit.Create(request.Name, request.Code);
        db.OrganicUnits.Add(unit);
        await db.SaveChangesAsync(cancellationToken);
        return unit.Id;
    }
}
