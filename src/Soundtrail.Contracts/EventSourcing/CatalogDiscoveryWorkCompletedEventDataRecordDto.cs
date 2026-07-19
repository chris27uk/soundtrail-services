namespace Soundtrail.Contracts.EventSourcing;

public sealed record CatalogDiscoveryWorkCompletedEventDataRecordDto(
    string MusicCatalogId,
    string Priority,
    string Reason,
    DateTimeOffset CompletedAtUtc) : RavenEventBodyDto;
