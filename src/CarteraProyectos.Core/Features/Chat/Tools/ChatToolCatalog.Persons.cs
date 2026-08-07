using System.Text.Json;
using CarteraProyectos.Core.Features.Agent;
using MediatR;

namespace CarteraProyectos.Core.Features.Chat.Tools;

public static partial class ChatToolCatalog
{
    private static ChatToolEntry GetPersons() => new(
        Name: "get_persons",
        Description: "Lista las personas registradas con su rol (Desarrollador/Gestor), si están activas y si ya han iniciado sesión alguna vez. Úsalo para buscar a una persona por nombre o email antes de editarla o asignarle trabajo.",
        ParametersJson: """{"type":"object","properties":{"includeInactive":{"type":"boolean","description":"Incluir personas inactivas (por defecto false)."}},"required":[]}""",
        Execute: async (args, personId, sender, ct) =>
            await sender.Send(new AgentGetPersonsQuery(GetBool(args, "includeInactive", false)), ct));

    private static ChatToolEntry CreatePerson() => new(
        Name: "create_person",
        Description: "Pre-registra una persona que se vinculará con su cuenta SSO en su primer inicio de sesión. role: Desarrollador o Gestor. Solo el Gestor puede hacerlo.",
        ParametersJson: """{"type":"object","properties":{"name":{"type":"string","description":"Nombre completo."},"email":{"type":"string","description":"Email corporativo."},"role":{"type":"string","description":"Desarrollador o Gestor."}},"required":["name","email","role"]}""",
        Execute: async (args, personId, sender, ct) =>
        {
            var id = await sender.Send(new AgentCreatePersonCommand(
                personId,
                GetString(args, "name"),
                GetString(args, "email"),
                GetString(args, "role")), ct);
            return new { id, message = $"Persona '{GetString(args, "name")}' pre-registrada con ID {id}." };
        });

    private static ChatToolEntry UpdatePerson() => new(
        Name: "update_person",
        Description: "Actualiza nombre, email y rol de una persona. Solo Gestor.",
        ParametersJson: """{"type":"object","properties":{"id":{"type":"integer","description":"ID de la persona."},"name":{"type":"string","description":"Nombre completo."},"email":{"type":"string","description":"Email corporativo."},"role":{"type":"string","description":"Desarrollador o Gestor."}},"required":["id","name","email","role"]}""",
        Execute: async (args, personId, sender, ct) =>
        {
            await sender.Send(new AgentUpdatePersonCommand(
                personId,
                GetInt(args, "id"),
                GetString(args, "name"),
                GetString(args, "email"),
                GetString(args, "role")), ct);
            return new { message = $"Persona {GetInt(args, "id")} actualizada." };
        });

    private static ChatToolEntry SetPersonActive() => new(
        Name: "set_person_active",
        Description: "Activa o desactiva una persona. Las personas inactivas no aparecen en listados ni pueden recibir tareas. Solo Gestor. Un Gestor no puede desactivarse a sí mismo.",
        ParametersJson: """{"type":"object","properties":{"id":{"type":"integer","description":"ID de la persona."},"isActive":{"type":"boolean","description":"true para activar, false para desactivar."}},"required":["id","isActive"]}""",
        Execute: async (args, personId, sender, ct) =>
        {
            var targetId = GetInt(args, "id");
            var isActive = GetBool(args, "isActive", true);
            await sender.Send(new AgentSetPersonActiveCommand(personId, targetId, isActive), ct);
            var action = isActive ? "activada" : "desactivada";
            return new { message = $"Persona {targetId} {action}." };
        });
}
