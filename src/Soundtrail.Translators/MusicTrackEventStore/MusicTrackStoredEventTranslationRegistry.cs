using Soundtrail.Domain.Catalog.Events;

namespace Soundtrail.Translators.MusicTrackEventStore;

public sealed class MusicTrackStoredEventTranslationRegistry
{
    private readonly Dictionary<Type, MusicTrackStoredEventRegistration> registrationsByDomainType = [];
    private readonly Dictionary<string, MusicTrackStoredEventRegistration> registrationsByEventType =
        new(StringComparer.Ordinal);

    public void Register<TDomainEvent, TBodyDto>(
        string eventType,
        Func<TDomainEvent, TBodyDto> toDto,
        Func<TBodyDto, TDomainEvent> toDomainObject,
        Func<TDomainEvent, DateTimeOffset> occurredAtUtc,
        Func<TDomainEvent, string?>? correlationId = null)
        where TDomainEvent : class, IMusicTrackEvent
        where TBodyDto : class
    {
        var registration = new MusicTrackStoredEventRegistration(
            eventType,
            typeof(TDomainEvent),
            typeof(TBodyDto),
            source => toDto((TDomainEvent)source),
            source => toDomainObject((TBodyDto)source),
            source => occurredAtUtc((TDomainEvent)source),
            source => correlationId?.Invoke((TDomainEvent)source));

        registrationsByDomainType[typeof(TDomainEvent)] = registration;
        registrationsByEventType[eventType] = registration;
    }

    public MusicTrackStoredEventRegistration GetByDomainType(Type domainType)
    {
        if (registrationsByDomainType.TryGetValue(domainType, out var registration))
        {
            return registration;
        }

        throw new InvalidOperationException($"No music track stored event registration exists for domain type '{domainType.FullName}'.");
    }

    public MusicTrackStoredEventRegistration GetByEventType(string eventType)
    {
        if (registrationsByEventType.TryGetValue(eventType, out var registration))
        {
            return registration;
        }

        throw new InvalidOperationException($"No music track stored event registration exists for event type '{eventType}'.");
    }
}
