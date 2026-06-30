namespace Soundtrail.Contracts.EventSourcing;

public sealed record MusicCatalogLookupStartedEventDataRecordDto(
    string MusicCatalogId,
    string Priority,
    DateTimeOffset StartedAtUtc) : RavenEventBodyDto;
