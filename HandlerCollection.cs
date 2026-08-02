using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

public interface IProjectionHandler<TMessage> where TMessage : IMessage
{
    Task HandleAsync(TMessage message, CancellationToken cancellationToken);
}

public sealed class HandlerCollection
{
    private static readonly IReadOnlyDictionary<Type, IReadOnlyList<object>> _handlersByMessageType;
    
    static HandlerCollection()
    {
        // Use Scrutor's container builder for full discovery
        var container = new ServiceCollection();
        
        // Register all assemblies that contain handlers
        var assemblies = new[]
        {
            typeof(HandlerCollection).Assembly,
            typeof(GetTracksForPlaylistHandler).Assembly, // Add relevant assemblies
            // Add other relevant assemblies here
        };
        
        // Register all IProjectionHandler implementations using Scrutor-like approach
        foreach (var assembly in assemblies)
        {
            var handlerTypes = assembly.GetTypes()
                .Where(t => !t.IsAbstract && !t.IsInterface)
                .Where(t => t.GetInterfaces()
                    .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IProjectionHandler<>)));
            
            foreach (var handlerType in handlerTypes)
            {
                var messageInterface = handlerType.GetInterfaces()
                    .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IProjectionHandler<>));
                
                var messageType = messageInterface.GetGenericArguments()[0];
                
                // Register the handler type
                container.AddTransient(handlerType);
            }
        }
        
        var serviceProvider = container.BuildServiceProvider();
        
        // Build the mapping from resolved services
        _handlersByMessageType = BuildHandlerMapping(serviceProvider, assemblies);
    }
    
    private static IReadOnlyDictionary<Type, IReadOnlyList<object>> BuildHandlerMapping(
        IServiceProvider serviceProvider, 
        Assembly[] assemblies)
    {
        var handlersByMessageType = new Dictionary<Type, List<object>>();
        
        foreach (var assembly in assemblies)
        {
            var handlerTypes = assembly.GetTypes()
                .Where(t => !t.IsAbstract && !t.IsInterface)
                .Where(t => t.GetInterfaces()
                    .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IProjectionHandler<>)));
            
            foreach (var handlerType in handlerTypes)
            {
                var messageInterface = handlerType.GetInterfaces()
                    .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IProjectionHandler<>));
                
                var messageType = messageInterface.GetGenericArguments()[0];
                
                if (!handlersByMessageType.TryGetValue(messageType, out var handlers))
                {
                    handlers = new List<object>();
                    handlersByMessageType[messageType] = handlers;
                }
                
                // Resolve instance through service provider
                handlers.Add(serviceProvider.GetRequiredService(handlerType));
            }
        }
        
        return handlersByMessageType.ToDictionary(kvp => kvp.Key, kvp => (IReadOnlyList<object>)kvp.Value);
    }

    public async Task HandleAsync<TMessage>(TMessage message, CancellationToken cancellationToken) 
        where TMessage : IMessage
    {
        // Direct invocation - no runtime reflection
        if (_handlersByMessageType.TryGetValue(typeof(TMessage), out var handlers))
        {
            foreach (var handler in handlers)
            {
                await ((IProjectionHandler<TMessage>)handler).HandleAsync(message, cancellationToken);
            }
        }
    }
}
