using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

public sealed class HandlerCollection
{
    private static readonly IReadOnlyDictionary<Type, IReadOnlyList<object>> _handlersByMessageType;
    
    static HandlerCollection()
    {
        // Discover handlers once at static initialization time using Scrutor-like approach
        var assemblies = new[]
        {
            typeof(HandlerCollection).Assembly,
            // Add other relevant assemblies here
        };
        
        var handlerTypes = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .Where(t => t.GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IProjectionHandler<>)))
            .ToList();

        var handlersByMessageType = new Dictionary<Type, List<object>>();
        
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
            
            handlers.Add(Activator.CreateInstance(handlerType)!);
        }
        
        _handlersByMessageType = handlersByMessageType.ToDictionary(kvp => kvp.Key, kvp => (IReadOnlyList<object>)kvp.Value);
    }

    public async Task HandleAsync<TMessage>(TMessage message, CancellationToken cancellationToken) 
        where TMessage : IMessage
    {
        if (_handlersByMessageType.TryGetValue(typeof(TMessage), out var handlers))
        {
            foreach (var handler in handlers)
            {
                var handlerInterface = handler.GetType().GetInterfaces()
                    .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IProjectionHandler<>));
                
                var method = handlerInterface.GetMethod(nameof(IProjectionHandler<IMessage>.HandleAsync));
                await (Task)method!.Invoke(handler, new object[] { message, cancellationToken })!;
            }
        }
    }
}
