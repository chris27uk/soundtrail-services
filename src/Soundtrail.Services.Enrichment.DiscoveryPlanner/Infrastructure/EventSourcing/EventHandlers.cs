namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Infrastructure.EventSourcing;

public sealed class EventHandlers
{
    private readonly Dictionary<Type, Action<object>> handlers = [];

    public void Register<TEvent>(Action<TEvent> handler)
    {
        this.handlers[typeof(TEvent)] = @event => handler((TEvent)@event);
    }

    public void Handle(object @event)
    {
        var eventType = @event.GetType();
        if (!this.handlers.TryGetValue(eventType, out var handler))
        {
            throw new InvalidOperationException($"No handler registered for event type {eventType.Name}.");
        }

        handler(@event);
    }
}
