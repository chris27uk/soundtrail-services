namespace Soundtrail.Contracts.EventSourcing;

public sealed record ProviderReferenceLookupFailedEventDataRecordDto(
    string? MusicCatalogId,
    string Provider,
    string SourceProvider,
    DateTimeOffset ObservedAt) : RavenEventBodyDto;
