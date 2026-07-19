namespace Soundtrail.Contracts.EventSourcing;

public sealed record ProviderReferenceDiscoveredEventDataRecordDto(
    string? MusicCatalogId,
    string? ArtistId,
    string Provider,
    string? ExternalId,
    string Url,
    string SourceProvider,
    DateTimeOffset ObservedAt) : RavenEventBodyDto;
