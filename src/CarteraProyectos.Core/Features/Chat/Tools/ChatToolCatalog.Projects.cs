using System.Text.Json;
using CarteraProyectos.Core.Features.Agent;
using MediatR;

namespace CarteraProyectos.Core.Features.Chat.Tools;

public static partial class ChatToolCatalog
{
    private static ChatToolEntry UpdateProjectStatus() => new(
        Name: "update_project_status",
        Description: "Cambia el estado de un proyecto. Estados: Stopped, PlanningWithClient, WaitingForDevelopers, PlanningSprint, InSprint, DevelopmentOutsideSprint, InTesting, Completed, PostponedByClient. El grafo de transiciones se valida en el servidor (para Completed todos los sprints deben estar completados y las tareas Done o Discarded). Pueden hacerlo el Gestor y cualquier miembro de un equipo asignado al proyecto. IMPORTANTE: confirma siempre con el usuario antes de ejecutar.",
        ParametersJson: """{"type":"object","properties":{"id":{"type":"integer","description":"ID del proyecto."},"status":{"type":"string","description":"Nuevo estado del proyecto."}},"required":["id","status"]}""",
        Execute: async (args, personId, sender, ct) =>
        {
            await sender.Send(new AgentTransitionProjectStatusCommand(personId, GetInt(args, "id"), GetString(args, "status")), ct);
            return new { message = $"Proyecto {GetInt(args, "id")} transicionado a estado '{GetString(args, "status")}'." };
        });

    private static ChatToolEntry CreateProject() => new(
        Name: "create_project",
        Description: "Crea un nuevo proyecto en la cartera. Solo el Gestor puede hacerlo. complexity: VerySmall, Small, Medium, Large, VeryLarge. Fechas en formato yyyy-MM-dd. businessValue (valor de negocio) y groupPriority entre 1 y 5. El proyecto se crea en estado Stopped; usa update_project_status para moverlo. IMPORTANTE: confirma siempre con el usuario antes de ejecutar.",
        ParametersJson: """{"type":"object","properties":{"title":{"type":"string","description":"Título del proyecto."},"description":{"type":"string","description":"Descripción (opcional)."},"requestingUnit":{"type":"string","description":"Unidad solicitante (opcional)."},"complexity":{"type":"string","description":"VerySmall, Small, Medium, Large, VeryLarge."},"portfolioYear":{"type":"integer","description":"Año de cartera (opcional)."},"startDate":{"type":"string","description":"Fecha inicio yyyy-MM-dd (opcional)."},"endDate":{"type":"string","description":"Fecha fin yyyy-MM-dd (opcional)."},"beneficiaryCount":{"type":"integer","description":"Número de beneficiarios (opcional)."},"promoterId":{"type":"integer","description":"ID del promotor (opcional)."},"organicUnitId":{"type":"integer","description":"ID de la unidad orgánica (opcional)."},"groupPriority":{"type":"integer","description":"Prioridad 1-5 (opcional)."},"desiredDeploymentDate":{"type":"string","description":"Fecha de despliegue deseada yyyy-MM-dd (opcional)."},"specificationsUrl":{"type":"string","description":"URL de especificaciones (opcional)."},"epicUrl":{"type":"string","description":"URL de épicas externas (opcional)."},"estimatedBudget":{"type":"number","description":"Presupuesto estimado (opcional)."},"businessValue":{"type":"integer","description":"Valor de negocio 1-5 (opcional)."}},"required":["title","complexity"]}""",
        Execute: async (args, personId, sender, ct) =>
        {
            var id = await sender.Send(new AgentCreateProjectCommand(
                personId,
                GetString(args, "title"),
                GetStringOrNull(args, "description"),
                GetStringOrNull(args, "requestingUnit"),
                GetString(args, "complexity"),
                GetIntOrNull(args, "portfolioYear"),
                GetDateOrNull(args, "startDate"),
                GetDateOrNull(args, "endDate"),
                GetIntOrNull(args, "beneficiaryCount"),
                GetIntOrNull(args, "promoterId"),
                GetIntOrNull(args, "organicUnitId"),
                GetIntOrNull(args, "groupPriority"),
                GetDateOrNull(args, "desiredDeploymentDate"),
                GetStringOrNull(args, "specificationsUrl"),
                GetStringOrNull(args, "epicUrl"),
                GetDecimalOrNull(args, "estimatedBudget"),
                GetIntOrNull(args, "businessValue")), ct);
            return new { id, message = $"Proyecto '{GetString(args, "title")}' creado con ID {id}." };
        });

    private static ChatToolEntry UpdateProject() => new(
        Name: "update_project",
        Description: "Actualización parcial de un proyecto: solo se modifican los campos enviados; los omitidos conservan su valor actual (no es posible vaciar un campo con esta tool). Solo el Gestor puede hacerlo. No cambia el estado del proyecto: para eso usa update_project_status. complexity: VerySmall, Small, Medium, Large, VeryLarge. Fechas en formato yyyy-MM-dd. IMPORTANTE: confirma siempre con el usuario antes de ejecutar.",
        ParametersJson: """{"type":"object","properties":{"id":{"type":"integer","description":"ID del proyecto."},"title":{"type":"string","description":"Título (opcional)."},"description":{"type":"string","description":"Descripción (opcional)."},"requestingUnit":{"type":"string","description":"Unidad solicitante (opcional)."},"complexity":{"type":"string","description":"VerySmall, Small, Medium, Large, VeryLarge (opcional)."},"portfolioYear":{"type":"integer","description":"Año de cartera (opcional)."},"startDate":{"type":"string","description":"Fecha inicio yyyy-MM-dd (opcional)."},"endDate":{"type":"string","description":"Fecha fin yyyy-MM-dd (opcional)."},"beneficiaryCount":{"type":"integer","description":"Número de beneficiarios (opcional)."},"promoterId":{"type":"integer","description":"ID del promotor (opcional)."},"organicUnitId":{"type":"integer","description":"ID de la unidad orgánica (opcional)."},"groupPriority":{"type":"integer","description":"Prioridad 1-5 (opcional)."},"desiredDeploymentDate":{"type":"string","description":"Fecha de despliegue deseada yyyy-MM-dd (opcional)."},"specificationsUrl":{"type":"string","description":"URL de especificaciones (opcional)."},"epicUrl":{"type":"string","description":"URL de épicas externas (opcional)."},"estimatedBudget":{"type":"number","description":"Presupuesto estimado (opcional)."},"businessValue":{"type":"integer","description":"Valor de negocio 1-5 (opcional)."}},"required":["id"]}""",
        Execute: async (args, personId, sender, ct) =>
        {
            await sender.Send(new AgentUpdateProjectCommand(
                personId,
                GetInt(args, "id"),
                GetStringOrNull(args, "title"),
                GetStringOrNull(args, "description"),
                GetStringOrNull(args, "requestingUnit"),
                GetStringOrNull(args, "complexity"),
                GetIntOrNull(args, "portfolioYear"),
                GetDateOrNull(args, "startDate"),
                GetDateOrNull(args, "endDate"),
                GetIntOrNull(args, "beneficiaryCount"),
                GetIntOrNull(args, "promoterId"),
                GetIntOrNull(args, "organicUnitId"),
                GetIntOrNull(args, "groupPriority"),
                GetDateOrNull(args, "desiredDeploymentDate"),
                GetStringOrNull(args, "specificationsUrl"),
                GetStringOrNull(args, "epicUrl"),
                GetDecimalOrNull(args, "estimatedBudget"),
                GetIntOrNull(args, "businessValue")), ct);
            return new { message = $"Proyecto {GetInt(args, "id")} actualizado." };
        });

    private static ChatToolEntry GetProjectRisks() => new(
        Name: "get_project_risks",
        Description: "Riesgos del proyecto con probabilidad × impacto = severidad (1-9) y estado (Open/Mitigated/Closed).",
        ParametersJson: """{"type":"object","properties":{"id":{"type":"integer","description":"ID del proyecto."}},"required":["id"]}""",
        Execute: async (args, personId, sender, ct) =>
            await sender.Send(new AgentGetProjectRisksQuery(GetInt(args, "id")), ct));

    private static ChatToolEntry AddProjectRisk() => new(
        Name: "add_project_risk",
        Description: "Registra un riesgo. probability e impact: Low, Medium o High. Gestor o miembro del equipo del proyecto.",
        ParametersJson: """{"type":"object","properties":{"id":{"type":"integer","description":"ID del proyecto."},"description":{"type":"string","description":"Descripción del riesgo."},"probability":{"type":"string","description":"Low, Medium o High."},"impact":{"type":"string","description":"Low, Medium o High."},"mitigationPlan":{"type":"string","description":"Plan de mitigación (opcional)."}},"required":["id","description","probability","impact"]}""",
        Execute: async (args, personId, sender, ct) =>
        {
            var riskId = await sender.Send(new AgentAddProjectRiskCommand(
                personId,
                GetInt(args, "id"),
                GetString(args, "description"),
                GetString(args, "probability"),
                GetString(args, "impact"),
                GetStringOrNull(args, "mitigationPlan")), ct);
            return new { id = riskId, message = $"Riesgo creado en el proyecto {GetInt(args, "id")}." };
        });

    private static ChatToolEntry UpdateProjectRisk() => new(
        Name: "update_project_risk",
        Description: "Actualiza un riesgo: descripción, niveles, plan de mitigación y estado (Open, Mitigated, Closed).",
        ParametersJson: """{"type":"object","properties":{"id":{"type":"integer","description":"ID del proyecto."},"riskId":{"type":"integer","description":"ID del riesgo."},"description":{"type":"string","description":"Descripción del riesgo."},"probability":{"type":"string","description":"Low, Medium o High."},"impact":{"type":"string","description":"Low, Medium o High."},"mitigationPlan":{"type":"string","description":"Plan de mitigación (opcional)."},"status":{"type":"string","description":"Open, Mitigated o Closed."}},"required":["id","riskId","description","probability","impact","status"]}""",
        Execute: async (args, personId, sender, ct) =>
        {
            await sender.Send(new AgentUpdateProjectRiskCommand(
                personId,
                GetInt(args, "id"),
                GetInt(args, "riskId"),
                GetString(args, "description"),
                GetString(args, "probability"),
                GetString(args, "impact"),
                GetStringOrNull(args, "mitigationPlan"),
                GetString(args, "status")), ct);
            return new { message = $"Riesgo {GetInt(args, "riskId")} del proyecto {GetInt(args, "id")} actualizado." };
        });

    private static ChatToolEntry GetProjectDependencies() => new(
        Name: "get_project_dependencies",
        Description: "Dependencias del proyecto: de qué proyectos depende y qué proyectos dependen de él.",
        ParametersJson: """{"type":"object","properties":{"id":{"type":"integer","description":"ID del proyecto."}},"required":["id"]}""",
        Execute: async (args, personId, sender, ct) =>
            await sender.Send(new AgentGetProjectDependenciesQuery(GetInt(args, "id")), ct));

    private static ChatToolEntry AddProjectDependency() => new(
        Name: "add_project_dependency",
        Description: "Registra que este proyecto depende de otro. El servidor rechaza autodependencias, duplicados y ciclos directos.",
        ParametersJson: """{"type":"object","properties":{"id":{"type":"integer","description":"ID del proyecto dependiente."},"dependsOnProjectId":{"type":"integer","description":"ID del proyecto del que depende."},"description":{"type":"string","description":"Descripción de la dependencia (opcional)."}},"required":["id","dependsOnProjectId"]}""",
        Execute: async (args, personId, sender, ct) =>
        {
            var depId = await sender.Send(new AgentAddProjectDependencyCommand(
                personId,
                GetInt(args, "id"),
                GetInt(args, "dependsOnProjectId"),
                GetStringOrNull(args, "description")), ct);
            return new { id = depId, message = $"Dependencia creada en el proyecto {GetInt(args, "id")}." };
        });

    private static ChatToolEntry AssignProjectTeam() => new(
        Name: "assign_project_team",
        Description: "Asigna un equipo a un proyecto o actualiza si ya estaba asignado. Solo puede hacerlo el Gestor. Solo puede haber un equipo primario por proyecto. IMPORTANTE: confirma siempre con el usuario antes de ejecutar.",
        ParametersJson: """{"type":"object","properties":{"projectId":{"type":"integer","description":"ID del proyecto."},"teamId":{"type":"integer","description":"ID del equipo."},"isPrimary":{"type":"boolean","description":"Si true, este equipo pasa a ser el equipo primario del proyecto."}},"required":["projectId","teamId","isPrimary"]}""",
        Execute: async (args, personId, sender, ct) =>
        {
            await sender.Send(new AgentAssignProjectTeamCommand(
                personId,
                GetInt(args, "projectId"),
                GetInt(args, "teamId"),
                GetBool(args, "isPrimary")), ct);
            return new { message = $"Equipo {GetInt(args, "teamId")} asignado al proyecto {GetInt(args, "projectId")}." };
        });
}
