namespace Soundtrail.Adapters.TypeRegistry;

internal sealed record StoredEventTypeRegistration(
    string EventType,
    Type DomainType,
    Type DtoType,
    Func<object, DateTimeOffset> OccurredAtUtc,
    Func<object, string?> CorrelationId);
