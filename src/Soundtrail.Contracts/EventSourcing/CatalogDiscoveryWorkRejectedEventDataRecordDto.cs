namespace Soundtrail.Contracts.EventSourcing;

public sealed record CatalogDiscoveryWorkRejectedEventDataRecordDto(
    string MusicCatalogId,
    string Priority,
    string Reason,
    DateTimeOffset RejectedAtUtc,
    int? SearchTypes = null) : RavenEventBodyDto;
