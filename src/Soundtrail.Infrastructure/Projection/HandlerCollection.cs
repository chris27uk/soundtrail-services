using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Domain.Abstractions;

namespace Soundtrail.Adapters.Projection;

public sealed class HandlerCollection
{
    private readonly Dictionary<Type, List<Func<object, CancellationToken, Task>>> handlers = [];

    public HandlerCollection Register<TPayload>(Func<TPayload, CancellationToken, Task> handle)
    {
        ArgumentNullException.ThrowIfNull(handle);
        GetOrCreate(typeof(TPayload)).Add(
            (payload, cancellationToken) => handle((TPayload)payload, cancellationToken));
        return this;
    }

    public HandlerCollection RegisterHandler<TMessage>(IHandler<TMessage> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        Task Invoke(object payload, CancellationToken cancellationToken)
        {
            if (payload is IncomingMessage<TMessage> incoming)
            {
                return handler.Handle(incoming, cancellationToken);
            }

            return handler.Handle(
                IncomingMessage<TMessage>.Create((TMessage)payload),
                cancellationToken);
        }

        GetOrCreate(typeof(TMessage)).Add(Invoke);
        GetOrCreate(typeof(IncomingMessage<TMessage>)).Add(Invoke);
        return this;
    }

    public async Task HandleAsync(object payload, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payload);

        if (!handlers.TryGetValue(payload.GetType(), out var registered))
        {
            throw new InvalidOperationException(
                $"No handler is registered for type '{payload.GetType().Name}'.");
        }

        foreach (var handle in registered)
        {
            await handle(payload, cancellationToken);
        }
    }

    public static void AddFromAssemblies(IServiceCollection services, params Type[] assemblyMarkers) =>
        AddProjectionHandlersFromAssemblies(services, assemblyMarkers);

    public static void AddProjectionHandlersFromAssemblies(
        IServiceCollection services,
        params Type[] assemblyMarkers)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assemblyMarkers);

        services.Scan(selector => selector
            .FromAssembliesOf(assemblyMarkers)
            .AddClasses(classes => classes.Where(static type => type
                .GetInterfaces()
                .Any(static contract =>
                    contract.IsGenericType
                    && contract.GetGenericTypeDefinition() == typeof(IProjectionEventHandler<>))))
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        var eventTypes = DiscoverPayloadTypes(
            assemblyMarkers,
            typeof(IProjectionEventHandler<>));
        foreach (var eventType in eventTypes)
        {
            services.AddSingleton<IHandlerCollectionContributor>(
                new ProjectionHandlerContributor(eventType));
        }

        EnsureHandlerCollectionRegistered(services);
    }

    public static void AddMessageHandlersFromAssemblies(
        IServiceCollection services,
        params Type[] assemblyMarkers)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assemblyMarkers);

        var messageTypes = DiscoverPayloadTypes(assemblyMarkers, typeof(IHandler<>));
        foreach (var messageType in messageTypes)
        {
            services.AddSingleton<IHandlerCollectionContributor>(
                new MessageHandlerContributor(messageType));
        }

        EnsureHandlerCollectionRegistered(services);
    }

    private List<Func<object, CancellationToken, Task>> GetOrCreate(Type payloadType)
    {
        if (handlers.TryGetValue(payloadType, out var registered))
        {
            return registered;
        }

        registered = [];
        handlers[payloadType] = registered;
        return registered;
    }

    private static void EnsureHandlerCollectionRegistered(IServiceCollection services)
    {
        services.TryAddScoped(sp =>
        {
            var collection = new HandlerCollection();
            foreach (var contributor in sp.GetServices<IHandlerCollectionContributor>())
            {
                contributor.Contribute(collection, sp);
            }

            return collection;
        });
    }

    private static IReadOnlyList<Type> DiscoverPayloadTypes(
        IReadOnlyList<Type> assemblyMarkers,
        Type openHandlerContract)
    {
        var payloadTypes = new HashSet<Type>();

        foreach (var assembly in assemblyMarkers.Select(static marker => marker.Assembly))
        {
            foreach (var type in assembly.GetTypes())
            {
                if (type is not { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false })
                {
                    continue;
                }

                foreach (var contract in type.GetInterfaces())
                {
                    if (contract.IsGenericType
                        && contract.GetGenericTypeDefinition() == openHandlerContract)
                    {
                        payloadTypes.Add(contract.GetGenericArguments()[0]);
                    }
                }
            }
        }

        return payloadTypes.ToArray();
    }
}

internal interface IHandlerCollectionContributor
{
    void Contribute(HandlerCollection collection, IServiceProvider serviceProvider);
}

internal sealed class ProjectionHandlerContributor(Type eventType) : IHandlerCollectionContributor
{
    public void Contribute(HandlerCollection collection, IServiceProvider serviceProvider)
    {
        var registerMethod = typeof(HandlerCollectionRegistrar)
            .GetMethod(nameof(HandlerCollectionRegistrar.RegisterProjectionHandlers), BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("Projection handler registrar could not be found.");

        registerMethod.MakeGenericMethod(eventType).Invoke(null, [collection, serviceProvider]);
    }
}

internal sealed class MessageHandlerContributor(Type messageType) : IHandlerCollectionContributor
{
    public void Contribute(HandlerCollection collection, IServiceProvider serviceProvider)
    {
        var registerMethod = typeof(HandlerCollectionRegistrar)
            .GetMethod(nameof(HandlerCollectionRegistrar.RegisterMessageHandler), BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("Message handler registrar could not be found.");

        registerMethod.MakeGenericMethod(messageType).Invoke(null, [collection, serviceProvider]);
    }
}

internal static class HandlerCollectionRegistrar
{
    public static void RegisterProjectionHandlers<TEvent>(
        HandlerCollection collection,
        IServiceProvider serviceProvider)
    {
        foreach (var handler in serviceProvider.GetServices<IProjectionEventHandler<TEvent>>())
        {
            collection.Register<TEvent>(handler.HandleAsync);
        }
    }

    public static void RegisterMessageHandler<TMessage>(
        HandlerCollection collection,
        IServiceProvider serviceProvider)
    {
        var handler = serviceProvider.GetService<IHandler<TMessage>>();
        if (handler is not null)
        {
            collection.RegisterHandler(handler);
        }
    }
}

public interface IProjectionEventHandler<in TEvent>
{
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
}
