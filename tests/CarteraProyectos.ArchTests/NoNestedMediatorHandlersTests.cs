using System.Reflection;
using MediatR;
using Shouldly;

namespace CarteraProyectos.ArchTests;

/// <summary>
/// Regla arquitectónica: ningún IRequestHandler puede depender de ISender o IMediator
/// en su constructor, con la única excepción documentada de SendChatMessageHandler,
/// que es el orquestador del bucle de tool-calling del agente IA y necesita despachar
/// dinámicamente los Agent*Command que decide el modelo.
///
/// Esta regla previene el patrón de "handlers que llaman a otros handlers" a través de
/// MediatR, que dificulta la trazabilidad y viola la separación de responsabilidades.
/// Si necesitas reutilizar lógica entre handlers, extráela a un servicio de dominio.
/// </summary>
public class NoNestedMediatorHandlersTests
{
    /// <summary>
    /// Única excepción permitida: el orquestador del chat, que despacha Agent*Commands
    /// de forma dinámica según lo que decida el modelo de IA en cada iteración.
    /// </summary>
    private const string AllowedExceptionHandlerName = "SendChatMessageHandler";

    [Fact]
    public void Handlers_do_not_depend_on_ISender_or_IMediator()
    {
        // Carga el ensamblado de Core a través de un tipo público conocido
        var coreAssembly = typeof(CarteraProyectos.Core.Features.Chat.SendChatMessageCommand).Assembly;

        // Busca todas las clases concretas que implementan IRequestHandler<> o IRequestHandler<,>
        // excluyendo la excepción documentada
        var handlerTypes = coreAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(t => t.GetInterfaces().Any(IsMediatrHandlerInterface))
            .Where(t => t.Name != AllowedExceptionHandlerName)
            .ToList();

        // Detecta handlers que inyectan ISender o IMediator en su constructor
        var offenders = handlerTypes
            .Where(HasSenderOrMediatorDependency)
            .Select(t => t.FullName!)
            .OrderBy(n => n)
            .ToList();

        offenders.ShouldBeEmpty(
            $"Los siguientes IRequestHandler dependen de ISender/IMediator en su constructor, " +
            $"lo cual reintroduce el anidamiento de handlers que este proyecto prohíbe. " +
            $"Única excepción documentada: {AllowedExceptionHandlerName} (orquestador del chat). " +
            $"Para reutilizar lógica, extráela a un servicio de dominio en lugar de llamar a otro handler. " +
            $"Infractores: {string.Join(", ", offenders)}");
    }

    private static bool IsMediatrHandlerInterface(Type i) =>
        i.IsGenericType &&
        (i.GetGenericTypeDefinition() == typeof(IRequestHandler<>) ||
         i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>));

    private static bool HasSenderOrMediatorDependency(Type handlerType) =>
        handlerType.GetConstructors()
            .SelectMany(c => c.GetParameters())
            .Any(p => p.ParameterType == typeof(ISender) ||
                      p.ParameterType == typeof(IMediator));
}
