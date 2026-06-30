namespace Soundtrail.Contracts.EventSourcing;

public sealed record MusicCatalogLookupDeferredEventDataRecordDto(
    string MusicCatalogId,
    string Priority,
    DateTimeOffset DeferredAtUtc,
    int? RetryAfterSeconds,
    DateTimeOffset? RetryAtUtc,
    string Reason,
    string? SearchCriteriaValue) : RavenEventBodyDto;
