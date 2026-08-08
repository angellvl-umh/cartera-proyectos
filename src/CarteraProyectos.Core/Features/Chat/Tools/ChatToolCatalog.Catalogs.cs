using CarteraProyectos.Core.Features.OrganicUnits;
using CarteraProyectos.Core.Features.Promoters;
using CarteraProyectos.Core.Features.Tags;

namespace CarteraProyectos.Core.Features.Chat.Tools;

public static partial class ChatToolCatalog
{
    private static ChatToolEntry GetPromoters() => new(
        Name: "get_promoters",
        Description: "Lista los promotores registrados en el sistema. Filtrable por nombre con el parámetro 'q' (búsqueda parcial, insensible a tildes y mayúsculas).",
        ParametersJson: """{"type":"object","properties":{"q":{"type":"string","description":"Búsqueda parcial por nombre (opcional)."},"page":{"type":"integer","description":"Página (default 1)."},"pageSize":{"type":"integer","description":"Tamaño de página (default 20, máx 100)."}},"required":[]}""",
        Execute: async (args, personId, sender, ct) =>
            await sender.Send(
                new GetPromotersQuery(
                    GetStringOrNull(args, "q"),
                    GetInt(args, "page", 1),
                    GetInt(args, "pageSize", 20)),
                ct));

    private static ChatToolEntry GetOrganicUnits() => new(
        Name: "get_organic_units",
        Description: "Lista las unidades orgánicas registradas en el sistema. Filtrable por nombre o código con el parámetro 'q' (búsqueda parcial, insensible a tildes y mayúsculas).",
        ParametersJson: """{"type":"object","properties":{"q":{"type":"string","description":"Búsqueda parcial por nombre o código (opcional)."},"page":{"type":"integer","description":"Página (default 1)."},"pageSize":{"type":"integer","description":"Tamaño de página (default 20, máx 100)."}},"required":[]}""",
        Execute: async (args, personId, sender, ct) =>
            await sender.Send(
                new GetOrganicUnitsQuery(
                    GetStringOrNull(args, "q"),
                    GetInt(args, "page", 1),
                    GetInt(args, "pageSize", 20)),
                ct));

    private static ChatToolEntry GetTags() => new(
        Name: "get_tags",
        Description: "Lista todas las etiquetas disponibles para asignar a proyectos.",
        ParametersJson: """{"type":"object","properties":{},"required":[]}""",
        Execute: async (args, personId, sender, ct) =>
            await sender.Send(new GetTagsQuery(), ct));
}
