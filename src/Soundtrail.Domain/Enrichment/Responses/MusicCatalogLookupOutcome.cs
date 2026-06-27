namespace Soundtrail.Domain.Enrichment.Responses;

public sealed record MusicCatalogLookupOutcome(
    MusicCatalogLookupOutcomeStatus Status,
    string? Reason,
    DateTimeOffset? RetryAt,
    int? RetryAfterSeconds)
{
    public static MusicCatalogLookupOutcome Completed() =>
        new(MusicCatalogLookupOutcomeStatus.Completed, null, null, null);

    public static MusicCatalogLookupOutcome Deferred(
        string reason,
        DateTimeOffset? retryAt,
        int? retryAfterSeconds) =>
        new(MusicCatalogLookupOutcomeStatus.Deferred, reason, retryAt, retryAfterSeconds);

    public static MusicCatalogLookupOutcome Duplicate() =>
        new(MusicCatalogLookupOutcomeStatus.Duplicate, null, null, null);

    public static MusicCatalogLookupOutcome Failed(string reason) =>
        new(MusicCatalogLookupOutcomeStatus.Failed, reason, null, null);
}
