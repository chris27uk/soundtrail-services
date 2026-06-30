namespace Soundtrail.Contracts.EventSourcing;

public sealed record MusicCatalogLookupFailedEventDataRecordDto(
    string MusicCatalogId,
    string Priority,
    DateTimeOffset FailedAtUtc,
    string Reason,
    string? SearchCriteriaValue) : RavenEventBodyDto;
