namespace Soundtrail.Services.Enrichment.Shared.EventSourcing;

public sealed class EventHandlers
{
    private readonly Dictionary<Type, Action<IDomainEvent>> handlers = [];

    public void Register<TEvent>(Action<TEvent> handler)
        where TEvent : IDomainEvent
    {
        handlers[typeof(TEvent)] = @event => handler((TEvent)@event);
    }

    public void Handle(IDomainEvent @event)
    {
        var eventType = @event.GetType();
        if (!handlers.TryGetValue(eventType, out var handler))
        {
            throw new InvalidOperationException($"No handler registered for event type {eventType.Name}.");
        }

        handler(@event);
    }
}
