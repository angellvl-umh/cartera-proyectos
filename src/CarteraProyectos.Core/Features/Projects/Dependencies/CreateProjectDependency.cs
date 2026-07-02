using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Interfaces;
using CarteraProyectos.Core.Features.Projects.Risks;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.Projects.Dependencies;

public record CreateProjectDependencyCommand(
    int ProjectId,
    int DependsOnProjectId,
    string? Description,
    int RequestingPersonId) : IRequest<int>;

public sealed class CreateProjectDependencyValidator : AbstractValidator<CreateProjectDependencyCommand>
{
    public CreateProjectDependencyValidator()
    {
        RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description is not null);
        RuleFor(x => x.DependsOnProjectId)
            .NotEqual(x => x.ProjectId)
            .WithMessage("Un proyecto no puede depender de sí mismo.");
    }
}

public sealed class CreateProjectDependencyHandler(IAppDbContext db) : IRequestHandler<CreateProjectDependencyCommand, int>
{
    public async Task<int> Handle(CreateProjectDependencyCommand request, CancellationToken cancellationToken)
    {
        // Validar que ambos proyectos existen
        if (!await db.Projects.AnyAsync(p => p.Id == request.ProjectId, cancellationToken))
            throw new KeyNotFoundException($"Proyecto con Id {request.ProjectId} no encontrado.");

        if (!await db.Projects.AnyAsync(p => p.Id == request.DependsOnProjectId, cancellationToken))
            throw new KeyNotFoundException($"Proyecto con Id {request.DependsOnProjectId} no encontrado.");

        // Validar permisos
        var requester = await db.Persons.FindAsync([request.RequestingPersonId], cancellationToken)
            ?? throw new KeyNotFoundException($"Persona con Id {request.RequestingPersonId} no encontrada.");

        await CreateProjectRiskHandler.AuthorizeRiskWriteAsync(db, request.ProjectId, requester, cancellationToken);

        // Validar que no exista duplicado
        var duplicate = await db.ProjectDependencies.AnyAsync(
            d => d.ProjectId == request.ProjectId && d.DependsOnProjectId == request.DependsOnProjectId,
            cancellationToken);
        if (duplicate)
            throw new InvalidOperationException(
                $"Ya existe una dependencia entre el proyecto {request.ProjectId} y el proyecto {request.DependsOnProjectId}.");

        // Validar que no genere ciclo directo (si B ya depende de A, no permitir A→B)
        var wouldCreateCycle = await db.ProjectDependencies.AnyAsync(
            d => d.ProjectId == request.DependsOnProjectId && d.DependsOnProjectId == request.ProjectId,
            cancellationToken);
        if (wouldCreateCycle)
            throw new InvalidOperationException(
                $"No se puede crear la dependencia: el proyecto {request.DependsOnProjectId} ya depende del proyecto {request.ProjectId}, " +
                "lo que generaría un ciclo directo.");

        var dependency = ProjectDependency.Create(request.ProjectId, request.DependsOnProjectId, request.Description);
        db.ProjectDependencies.Add(dependency);
        await db.SaveChangesAsync(cancellationToken);
        return dependency.Id;
    }
}
