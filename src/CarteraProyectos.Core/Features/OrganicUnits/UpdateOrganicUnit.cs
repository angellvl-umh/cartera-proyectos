using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.OrganicUnits;

public record UpdateOrganicUnitCommand(int Id, string Name, string? Code, int RequestingPersonId = 0) : IRequest;

public sealed class UpdateOrganicUnitValidator : AbstractValidator<UpdateOrganicUnitCommand>
{
    public UpdateOrganicUnitValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Code).MaximumLength(50).When(x => x.Code is not null);
    }
}

public sealed class UpdateOrganicUnitHandler(IAppDbContext db) : IRequestHandler<UpdateOrganicUnitCommand>
{
    public async Task Handle(UpdateOrganicUnitCommand request, CancellationToken cancellationToken)
    {
        var requester = await db.Persons.FindAsync([request.RequestingPersonId], cancellationToken)
            ?? throw new KeyNotFoundException($"Persona con Id {request.RequestingPersonId} no encontrada.");
        if (requester.Role != PersonRole.Gestor)
            throw new UnauthorizedAccessException("Solo el Gestor puede gestionar unidades orgánicas.");

        var unit = await db.OrganicUnits.FindAsync([request.Id], cancellationToken)
            ?? throw new KeyNotFoundException($"Unidad orgánica con Id {request.Id} no encontrada.");

        if (await db.OrganicUnits.AnyAsync(u => u.Name == request.Name && u.Id != request.Id, cancellationToken))
            throw new InvalidOperationException($"Ya existe una unidad orgánica con el nombre '{request.Name}'.");

        unit.Update(request.Name, request.Code);
        await db.SaveChangesAsync(cancellationToken);
    }
}
