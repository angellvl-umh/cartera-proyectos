namespace CarteraProyectos.Core.Domain;

public enum ProjectStatus
{
    Stopped,                  // PARADO
    PlanningWithClient,       // PLANIFICANDO CON CLIENTE
    WaitingForDevelopers,     // ESPERANDO DESARROLLADORES
    PlanningSprint,           // PLANIFICANDO SPRINT
    InSprint,                 // EN SPRINT
    DevelopmentOutsideSprint, // DESARROLLO FUERA DE SPRINT
    InTesting,                // EN PRUEBAS
    Completed,                // FINALIZADO
    PostponedByClient,        // POSPUESTO POR CLIENTE
}

public enum ProjectComplexity
{
    VerySmall, // MUY PEQUEÑO (≤ 2 semanas, ≤ 2 personas)
    Small,     // PEQUEÑO (≤ 1 mes, ≤ 2 personas)
    Medium,    // MEDIO (≤ 2 meses, ≤ 2 personas)
    Large,     // GRANDE (≤ 4 meses, ≤ 4 personas)
    VeryLarge, // MUY GRANDE (> 4 meses, > 4 personas)
}

public enum SiptGroup { WebTransversal, RRHH, Academico, Sede, Observatorio, InvestigacionEconomico }

public class Project
{
    public int Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? RequestingUnit { get; private set; }
    public ProjectComplexity Complexity { get; private set; }
    public ProjectStatus Status { get; private set; } = ProjectStatus.Stopped;
    public int? PortfolioYear { get; private set; }
    public DateOnly? StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }

    // Campos ampliados
    public int? PreviousReferenceId { get; private set; }
    public int? BeneficiaryCount { get; private set; }
    public int? PromoterId { get; private set; }
    public int? OrganicUnitId { get; private set; }
    public int? UorOrder { get; private set; }
    public int? GroupPriority { get; private set; }
    public SiptGroup? SiptGroup { get; private set; }
    public DateOnly? DesiredDeploymentDate { get; private set; }
    public string? SpecificationsUrl { get; private set; }
    public string? EpicUrl { get; private set; }
    public decimal? EstimatedBudget { get; private set; }
    public int? BusinessValue { get; private set; } // Valor de negocio percibido: escala 1–5

    // Navegación
    public Promoter? Promoter { get; private set; }
    public OrganicUnit? OrganicUnit { get; private set; }
    public ICollection<ProjectTeamAssignment> Teams { get; } = [];
    public ICollection<Epic> Epics { get; } = [];
    public ICollection<Tag> Tags { get; } = [];
    public ICollection<ProjectNote> Notes { get; } = [];
    public ICollection<ProjectWeeklyUpdate> WeeklyUpdates { get; } = [];
    public ICollection<ProjectRisk> Risks { get; } = [];
    public ICollection<ProjectDependency> Dependencies { get; } = [];

    private static readonly Dictionary<ProjectStatus, ProjectStatus[]> AllowedTransitions = new()
    {
        [ProjectStatus.Stopped]                  = [ProjectStatus.PlanningWithClient, ProjectStatus.PostponedByClient],
        [ProjectStatus.PlanningWithClient]       = [ProjectStatus.WaitingForDevelopers, ProjectStatus.PlanningSprint, ProjectStatus.DevelopmentOutsideSprint, ProjectStatus.Stopped, ProjectStatus.PostponedByClient],
        [ProjectStatus.WaitingForDevelopers]     = [ProjectStatus.PlanningSprint, ProjectStatus.DevelopmentOutsideSprint, ProjectStatus.PlanningWithClient, ProjectStatus.Stopped, ProjectStatus.PostponedByClient],
        [ProjectStatus.PlanningSprint]           = [ProjectStatus.InSprint, ProjectStatus.WaitingForDevelopers, ProjectStatus.Stopped, ProjectStatus.PostponedByClient],
        [ProjectStatus.InSprint]                 = [ProjectStatus.InTesting, ProjectStatus.PlanningSprint, ProjectStatus.DevelopmentOutsideSprint, ProjectStatus.Stopped, ProjectStatus.PostponedByClient],
        [ProjectStatus.DevelopmentOutsideSprint] = [ProjectStatus.InTesting, ProjectStatus.PlanningSprint, ProjectStatus.Stopped, ProjectStatus.PostponedByClient],
        [ProjectStatus.InTesting]                = [ProjectStatus.Completed, ProjectStatus.InSprint, ProjectStatus.DevelopmentOutsideSprint, ProjectStatus.Stopped, ProjectStatus.PostponedByClient],
        [ProjectStatus.PostponedByClient]        = [ProjectStatus.PlanningWithClient, ProjectStatus.PlanningSprint, ProjectStatus.DevelopmentOutsideSprint, ProjectStatus.Stopped],
        [ProjectStatus.Completed]                = [],
    };

    public static IReadOnlyList<ProjectStatus> GetAllowedTransitions(ProjectStatus from)
        => AllowedTransitions.TryGetValue(from, out var targets) ? targets : [];

    public void TransitionTo(ProjectStatus next)
    {
        if (!AllowedTransitions[Status].Contains(next))
            throw new InvalidOperationException($"No se puede transicionar el proyecto de '{Status}' a '{next}'.");
        Status = next;
    }

    /// <summary>
    /// Establece el estado directamente sin validar la máquina de estados.
    /// Solo debe usarse en seeds de datos y tests de integración.
    /// </summary>
    public void SetStatusDirectly(ProjectStatus status) => Status = status;

    public void Update(
        string title, string? description, string? requestingUnit,
        ProjectComplexity complexity, int? portfolioYear, DateOnly? startDate, DateOnly? endDate,
        int? previousReferenceId = null, int? beneficiaryCount = null,
        int? promoterId = null, int? organicUnitId = null, int? uorOrder = null,
        int? groupPriority = null, SiptGroup? siptGroup = null,
        DateOnly? desiredDeploymentDate = null,
        string? specificationsUrl = null, string? epicUrl = null,
        decimal? estimatedBudget = null, int? businessValue = null)
    {
        Title = title;
        Description = description;
        RequestingUnit = requestingUnit;
        Complexity = complexity;
        PortfolioYear = portfolioYear;
        StartDate = startDate;
        EndDate = endDate;
        PreviousReferenceId = previousReferenceId;
        BeneficiaryCount = beneficiaryCount;
        PromoterId = promoterId;
        OrganicUnitId = organicUnitId;
        UorOrder = uorOrder;
        GroupPriority = groupPriority;
        SiptGroup = siptGroup;
        DesiredDeploymentDate = desiredDeploymentDate;
        SpecificationsUrl = specificationsUrl;
        EpicUrl = epicUrl;
        EstimatedBudget = estimatedBudget;
        BusinessValue = businessValue;
    }

    public static Project Create(
        string title, string? description, string? requestingUnit,
        ProjectComplexity complexity, int? portfolioYear, DateOnly? startDate, DateOnly? endDate,
        int? previousReferenceId = null, int? beneficiaryCount = null,
        int? promoterId = null, int? organicUnitId = null, int? uorOrder = null,
        int? groupPriority = null, SiptGroup? siptGroup = null,
        DateOnly? desiredDeploymentDate = null,
        string? specificationsUrl = null, string? epicUrl = null,
        decimal? estimatedBudget = null, int? businessValue = null)
        => new()
        {
            Title = title,
            Description = description,
            RequestingUnit = requestingUnit,
            Complexity = complexity,
            PortfolioYear = portfolioYear,
            StartDate = startDate,
            EndDate = endDate,
            PreviousReferenceId = previousReferenceId,
            BeneficiaryCount = beneficiaryCount,
            PromoterId = promoterId,
            OrganicUnitId = organicUnitId,
            UorOrder = uorOrder,
            GroupPriority = groupPriority,
            SiptGroup = siptGroup,
            DesiredDeploymentDate = desiredDeploymentDate,
            SpecificationsUrl = specificationsUrl,
            EpicUrl = epicUrl,
            EstimatedBudget = estimatedBudget,
            BusinessValue = businessValue,
        };
}
