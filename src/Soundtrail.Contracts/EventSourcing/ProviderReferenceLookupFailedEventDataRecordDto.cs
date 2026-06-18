namespace Soundtrail.Contracts.EventSourcing;

public sealed record ProviderReferenceLookupFailedEventDataRecordDto(
    string Provider,
    string SourceProvider,
    DateTimeOffset ObservedAt);
