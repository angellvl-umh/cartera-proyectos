namespace CarteraProyectos.Core.Domain;

public enum ProjectStatus { Proposed, Approved, InProgress, Paused, Completed, Cancelled }
public enum ProjectComplexity { Low, Medium, High, VeryHigh }

public class Project
{
    public int Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string RequestingUnit { get; private set; } = string.Empty;
    public ProjectComplexity Complexity { get; private set; }
    public ProjectStatus Status { get; private set; } = ProjectStatus.Proposed;
    public int? PortfolioYear { get; private set; }
    public DateOnly? StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public ICollection<ProjectTeamAssignment> Teams { get; } = [];
    public ICollection<Epic> Epics { get; } = [];

    private static readonly Dictionary<ProjectStatus, HashSet<ProjectStatus>> AllowedTransitions = new()
    {
        [ProjectStatus.Proposed]    = [ProjectStatus.Approved, ProjectStatus.Cancelled],
        [ProjectStatus.Approved]    = [ProjectStatus.InProgress, ProjectStatus.Cancelled],
        [ProjectStatus.InProgress]  = [ProjectStatus.Paused, ProjectStatus.Completed, ProjectStatus.Cancelled],
        [ProjectStatus.Paused]      = [ProjectStatus.InProgress, ProjectStatus.Cancelled],
        [ProjectStatus.Completed]   = [],
        [ProjectStatus.Cancelled]   = [],
    };

    public bool CanTransitionTo(ProjectStatus next) =>
        AllowedTransitions.TryGetValue(Status, out var allowed) && allowed.Contains(next);

    public void TransitionTo(ProjectStatus next) => Status = next;

    public void Update(string title, string? description, string requestingUnit,
        ProjectComplexity complexity, int? portfolioYear, DateOnly? startDate, DateOnly? endDate)
    {
        Title = title;
        Description = description;
        RequestingUnit = requestingUnit;
        Complexity = complexity;
        PortfolioYear = portfolioYear;
        StartDate = startDate;
        EndDate = endDate;
    }

    public static Project Create(string title, string? description, string requestingUnit,
        ProjectComplexity complexity, int? portfolioYear, DateOnly? startDate, DateOnly? endDate)
        => new()
        {
            Title = title,
            Description = description,
            RequestingUnit = requestingUnit,
            Complexity = complexity,
            PortfolioYear = portfolioYear,
            StartDate = startDate,
            EndDate = endDate,
        };
}
