using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CarteraProyectos.Core.Features.Projects;

public interface IProjectLifecycleService
{
    Task<int> CreateAsync(CreateProjectCommand command, CancellationToken ct);
    Task UpdateAsync(UpdateProjectCommand command, CancellationToken ct);
    Task TransitionStatusAsync(TransitionProjectStatusCommand command, CancellationToken ct);
    Task AssignTeamAsync(AssignTeamToProjectCommand command, CancellationToken ct);
}

public sealed class ProjectLifecycleService(IAppDbContext db) : IProjectLifecycleService
{
    public async Task<int> CreateAsync(CreateProjectCommand request, CancellationToken ct)
    {
        if (request.RequestingPersonId > 0)
        {
            var requester = await db.Persons.FindAsync([request.RequestingPersonId], ct)
                ?? throw new KeyNotFoundException($"Persona con Id {request.RequestingPersonId} no encontrada.");
            if (requester.Role != PersonRole.Gestor)
                throw new UnauthorizedAccessException("Solo el Gestor puede crear proyectos.");
        }

        var project = Project.Create(
            request.Title, request.Description, request.RequestingUnit,
            request.Complexity, request.PortfolioYear, request.StartDate, request.EndDate,
            request.PreviousReferenceId, request.BeneficiaryCount,
            request.PromoterId, request.OrganicUnitId, request.UorOrder,
            request.GroupPriority,
            request.DesiredDeploymentDate, request.SpecificationsUrl, request.EpicUrl,
            request.EstimatedBudget, request.BusinessValue);

        if (request.TagIds is { Count: > 0 })
        {
            var tags = db.Tags.Where(t => request.TagIds.Contains(t.Id)).ToList();
            foreach (var tag in tags)
                ((ICollection<Tag>)project.Tags).Add(tag);
        }

        db.Projects.Add(project);
        db.ProjectStatusHistories.Add(ProjectStatusHistory.Create(project, null, project.Status, request.RequestingPersonId));
        await db.SaveChangesAsync(ct);

        if (request.TeamIds is { Count: > 0 })
        {
            foreach (var teamId in request.TeamIds)
            {
                db.ProjectTeamAssignments.Add(
                    ProjectTeamAssignment.Create(project.Id, teamId, isPrimary: teamId == request.PrimaryTeamId));
            }
            await db.SaveChangesAsync(ct);
        }

        return project.Id;
    }

    public async Task UpdateAsync(UpdateProjectCommand request, CancellationToken ct)
    {
        if (request.RequestingPersonId > 0)
        {
            var requester = await db.Persons.FindAsync([request.RequestingPersonId], ct)
                ?? throw new KeyNotFoundException($"Persona con Id {request.RequestingPersonId} no encontrada.");
            if (requester.Role != PersonRole.Gestor)
                throw new UnauthorizedAccessException("Solo el Gestor puede actualizar proyectos.");
        }

        var project = await db.Projects
            .Include(p => p.Tags)
            .FirstOrDefaultAsync(p => p.Id == request.Id, ct)
            ?? throw new KeyNotFoundException($"Proyecto con Id {request.Id} no encontrado.");

        project.Update(
            request.Title, request.Description, request.RequestingUnit,
            request.Complexity, request.PortfolioYear, request.StartDate, request.EndDate,
            request.PreviousReferenceId, request.BeneficiaryCount,
            request.PromoterId, request.OrganicUnitId, request.UorOrder,
            request.GroupPriority,
            request.DesiredDeploymentDate, request.SpecificationsUrl, request.EpicUrl,
            request.EstimatedBudget, request.BusinessValue);

        if (request.TagIds is not null)
        {
            var existingTags = ((ICollection<Tag>)project.Tags);
            existingTags.Clear();
            if (request.TagIds.Count > 0)
            {
                var tags = db.Tags.Where(t => request.TagIds.Contains(t.Id)).ToList();
                foreach (var tag in tags)
                    existingTags.Add(tag);
            }
        }

        if (request.TeamIds is not null)
        {
            var existingAssignments = await db.ProjectTeamAssignments
                .Where(a => a.ProjectId == request.Id)
                .ToListAsync(ct);
            db.ProjectTeamAssignments.RemoveRange(existingAssignments);

            foreach (var teamId in request.TeamIds)
            {
                db.ProjectTeamAssignments.Add(
                    ProjectTeamAssignment.Create(request.Id, teamId, isPrimary: teamId == request.PrimaryTeamId));
            }
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task TransitionStatusAsync(TransitionProjectStatusCommand request, CancellationToken ct)
    {
        var project = await db.Projects
            .FirstOrDefaultAsync(p => p.Id == request.ProjectId, ct)
            ?? throw new KeyNotFoundException($"Proyecto con Id {request.ProjectId} no encontrado.");

        var person = await db.Persons.FindAsync([request.RequestingPersonId], ct)
            ?? throw new KeyNotFoundException($"Persona con Id {request.RequestingPersonId} no encontrada.");

        await ProjectAuthorization.EnsureCanManageProjectAsync(db, project.Id, person, ct);

        if (request.NewStatus == ProjectStatus.Completed)
        {
            var hasUnfinishedSprints = await db.Sprints.AnyAsync(
                s => s.ProjectId == project.Id && s.Status != SprintStatus.Completed, ct);
            if (hasUnfinishedSprints)
                throw new InvalidOperationException("No se puede finalizar el proyecto: tiene sprints que no están en estado Completed.");

            var hasUnfinishedWorkItems = await db.WorkItems.AnyAsync(
                w => w.ProjectId == project.Id && w.Status != WorkItemStatus.Done && w.Status != WorkItemStatus.Discarded, ct);
            if (hasUnfinishedWorkItems)
                throw new InvalidOperationException("No se puede finalizar el proyecto: tiene tareas que no están en estado Done o Discarded.");
        }

        var oldStatus = project.Status;
        project.TransitionTo(request.NewStatus);
        db.ProjectStatusHistories.Add(ProjectStatusHistory.Create(project, oldStatus, request.NewStatus, request.RequestingPersonId));
        await db.SaveChangesAsync(ct);
    }

    public async Task AssignTeamAsync(AssignTeamToProjectCommand request, CancellationToken ct)
    {
        var requester = await db.Persons.FindAsync([request.RequestingPersonId], ct)
            ?? throw new KeyNotFoundException($"Persona con Id {request.RequestingPersonId} no encontrada.");
        if (requester.Role != PersonRole.Gestor)
            throw new UnauthorizedAccessException("Solo el Gestor puede asignar equipos a proyectos.");

        if (!await db.Projects.AnyAsync(p => p.Id == request.ProjectId, ct))
            throw new KeyNotFoundException($"Proyecto con Id {request.ProjectId} no encontrado.");

        if (!await db.Teams.AnyAsync(t => t.Id == request.TeamId, ct))
            throw new KeyNotFoundException($"Equipo con Id {request.TeamId} no encontrado.");

        var existing = await db.ProjectTeamAssignments
            .FirstOrDefaultAsync(a => a.ProjectId == request.ProjectId && a.TeamId == request.TeamId, ct);

        if (existing is not null)
        {
            existing.SetPrimary(request.IsPrimary);
        }
        else
        {
            db.ProjectTeamAssignments.Add(
                ProjectTeamAssignment.Create(request.ProjectId, request.TeamId, request.IsPrimary));
        }

        // Solo puede haber un equipo primario
        if (request.IsPrimary)
        {
            var others = await db.ProjectTeamAssignments
                .Where(a => a.ProjectId == request.ProjectId && a.TeamId != request.TeamId && a.IsPrimary)
                .ToListAsync(ct);
            foreach (var other in others) other.SetPrimary(false);
        }

        await db.SaveChangesAsync(ct);
    }
}
