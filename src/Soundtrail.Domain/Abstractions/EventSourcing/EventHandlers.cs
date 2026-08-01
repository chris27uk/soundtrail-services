namespace Soundtrail.Domain.Abstractions.EventSourcing;

public sealed class EventHandlers
{
    private readonly Dictionary<Type, List<HandlerRegistration>> handlers = [];

    public void Register<TEvent>(Action<TEvent> handler) where TEvent : IDomainEvent
    {
        ArgumentNullException.ThrowIfNull(handler);
        GetOrCreateRegistrations(typeof(TEvent)).Add(
            HandlerRegistration.ForSync(@event => handler((TEvent)@event)));
    }

    public void RegisterAsync<TEvent>(Func<TEvent, CancellationToken, Task> handler) where TEvent : IDomainEvent
    {
        ArgumentNullException.ThrowIfNull(handler);
        GetOrCreateRegistrations(typeof(TEvent)).Add(
            HandlerRegistration.ForAsync((@event, cancellationToken) => handler((TEvent)@event, cancellationToken)));
    }

    public void Handle(IDomainEvent @event)
    {
        var eventType = @event.GetType();
        if (!this.handlers.TryGetValue(eventType, out var registrations))
        {
            throw new InvalidOperationException($"No handler registered for event type {eventType.Name}.");
        }

        foreach (var registration in registrations)
        {
            if (registration.AsyncHandler is not null)
            {
                throw new InvalidOperationException(
                    $"Cannot synchronously handle event type {eventType.Name} because async handlers are registered.");
            }

            registration.SyncHandler!(@event);
        }
    }

    public async Task HandleAsync(IDomainEvent @event, CancellationToken cancellationToken = default)
    {
        var eventType = @event.GetType();
        if (!this.handlers.TryGetValue(eventType, out var registrations))
        {
            throw new InvalidOperationException($"No handler registered for event type {eventType.Name}.");
        }

        foreach (var registration in registrations)
        {
            if (registration.SyncHandler is not null)
            {
                registration.SyncHandler(@event);
                continue;
            }

            await registration.AsyncHandler!(@event, cancellationToken);
        }
    }

    private List<HandlerRegistration> GetOrCreateRegistrations(Type eventType)
    {
        if (this.handlers.TryGetValue(eventType, out var registrations))
        {
            return registrations;
        }

        registrations = [];
        this.handlers[eventType] = registrations;
        return registrations;
    }

    private sealed record HandlerRegistration(
        Action<IDomainEvent>? SyncHandler,
        Func<IDomainEvent, CancellationToken, Task>? AsyncHandler)
    {
        public static HandlerRegistration ForSync(Action<IDomainEvent> handler) => new(handler, null);

        public static HandlerRegistration ForAsync(Func<IDomainEvent, CancellationToken, Task> handler) => new(null, handler);
    }
}
