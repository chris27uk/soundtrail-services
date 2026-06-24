namespace Soundtrail.Translators.MusicTrackEventStore;

public sealed record MusicTrackStoredEventRegistration(
    string EventType,
    Type DomainType,
    Type BodyDtoType,
    Func<object, object> ToDto,
    Func<object, object> ToDomainObject,
    Func<object, DateTimeOffset> GetOccurredAtUtc,
    Func<object, string?> GetCorrelationId);
