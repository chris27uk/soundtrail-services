namespace Soundtrail.Domain.Responses;

public sealed record ReplayCatalogProjectionResult(
    int ReplayedStreamCount,
    int ReplayedEventCount);
