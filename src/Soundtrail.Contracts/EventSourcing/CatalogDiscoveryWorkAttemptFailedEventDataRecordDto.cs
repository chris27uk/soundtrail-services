namespace Soundtrail.Contracts.EventSourcing;

public sealed record CatalogDiscoveryWorkAttemptFailedEventDataRecordDto(
    string MusicCatalogId,
    string Reason,
    DateTimeOffset FailedAtUtc) : RavenEventBodyDto;
