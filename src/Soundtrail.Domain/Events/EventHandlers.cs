namespace Soundtrail.Domain.Events;

public sealed class EventHandlers<TAggregate>
{
    private readonly Dictionary<Type, Action<TAggregate, IDomainEvent>> handlers = [];

    public void Register<TEvent>(Action<TAggregate, TEvent> handler)
        where TEvent : IDomainEvent
    {
        handlers[typeof(TEvent)] = (aggregate, @event) => handler(aggregate, (TEvent)@event);
    }

    public void Handle(TAggregate aggregate, IDomainEvent @event)
    {
        var eventType = @event.GetType();
        if (!handlers.TryGetValue(eventType, out var handler))
        {
            throw new InvalidOperationException($"No handler registered for event type {eventType.Name}.");
        }

        handler(aggregate, @event);
    }
}
