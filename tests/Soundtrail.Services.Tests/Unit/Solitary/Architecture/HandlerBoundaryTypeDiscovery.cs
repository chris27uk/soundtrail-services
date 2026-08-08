using System.Reflection;
using Soundtrail.Adapters.Projection;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Abstractions.EventSourcing;

namespace Soundtrail.Services.Tests.Unit.Solitary.Architecture;

internal static class HandlerBoundaryTypeDiscovery
{
    public sealed record DiscoveredType(Type DomainType, string Source);

    public static IReadOnlyList<DiscoveredType> DiscoverRequiredTypes(
        Assembly orchestratorAssembly,
        Assembly schedulerAssembly,
        Assembly projectorAssembly,
        Assembly apiAssembly)
    {
        var discovered = new Dictionary<Type, HashSet<string>>();

        AddRange(
            discovered,
            DiscoverOpenGenericPayloads(orchestratorAssembly, typeof(IHandler<>), "handled"),
            includeWhen: RequiresTypeRegistryPair);

        AddRange(
            discovered,
            DiscoverOpenGenericPayloads(schedulerAssembly, typeof(IHandler<>), "handled"),
            includeWhen: RequiresTypeRegistryPair);

        AddRange(
            discovered,
            DiscoverOpenGenericPayloads(projectorAssembly, typeof(IProjectionEventHandler<>), "handled"),
            includeWhen: RequiresTypeRegistryPair);

        AddRange(
            discovered,
            DiscoverDomainMessageTypes("outbound"),
            includeWhen: static _ => true);

        AddRange(
            discovered,
            DiscoverApiResponses(apiAssembly, "api-response"),
            includeWhen: static _ => true);

        return discovered
            .OrderBy(static pair => pair.Key.FullName, StringComparer.Ordinal)
            .Select(static pair => new DiscoveredType(
                pair.Key,
                string.Join(", ", pair.Value.OrderBy(static source => source, StringComparer.Ordinal))))
            .ToArray();
    }

    public static Type UnwrapNullable(Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            return type.GetGenericArguments()[0];
        }

        return Nullable.GetUnderlyingType(type) ?? type;
    }

    private static bool RequiresTypeRegistryPair(Type type) =>
        typeof(IMessage).IsAssignableFrom(type) || typeof(IDomainEvent).IsAssignableFrom(type);

    private static IEnumerable<(Type Type, string Source)> DiscoverOpenGenericPayloads(
        Assembly assembly,
        Type openHandlerContract,
        string source)
    {
        foreach (var type in assembly.GetTypes())
        {
            if (type is not { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false })
            {
                continue;
            }

            foreach (var contract in type.GetInterfaces())
            {
                if (!contract.IsGenericType || contract.GetGenericTypeDefinition() != openHandlerContract)
                {
                    continue;
                }

                yield return (UnwrapNullable(contract.GetGenericArguments()[0]), source);
            }
        }
    }

    private static IEnumerable<(Type Type, string Source)> DiscoverApiResponses(Assembly assembly, string source)
    {
        var apiHandlerContract = typeof(IApiHandler<,>);

        foreach (var type in assembly.GetTypes())
        {
            if (type is not { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false })
            {
                continue;
            }

            foreach (var contract in type.GetInterfaces())
            {
                if (!contract.IsGenericType || contract.GetGenericTypeDefinition() != apiHandlerContract)
                {
                    continue;
                }

                yield return (UnwrapNullable(contract.GetGenericArguments()[1]), source);
            }
        }
    }

    // Outbound bus messages: all concrete IMessage types in Domain.
    // (IL newobj scanning proved brittle; Domain-wide discovery is the plan fallback and
    // still guarantees CommandBus.ToDto cannot fail for any sendable message.)
    private static IEnumerable<(Type Type, string Source)> DiscoverDomainMessageTypes(string source)
    {
        foreach (var type in typeof(IMessage).Assembly.GetTypes())
        {
            if (type is { IsClass: true, IsAbstract: false } && typeof(IMessage).IsAssignableFrom(type))
            {
                yield return (type, source);
            }
        }
    }

    private static void AddRange(
        Dictionary<Type, HashSet<string>> discovered,
        IEnumerable<(Type Type, string Source)> items,
        Func<Type, bool> includeWhen)
    {
        foreach (var (type, source) in items)
        {
            if (!includeWhen(type))
            {
                continue;
            }

            if (!discovered.TryGetValue(type, out var sources))
            {
                sources = [];
                discovered[type] = sources;
            }

            sources.Add(source);
        }
    }
}
