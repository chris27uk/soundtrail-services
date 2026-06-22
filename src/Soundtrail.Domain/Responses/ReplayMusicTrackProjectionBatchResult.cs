namespace Soundtrail.Domain.Responses;

public sealed record ReplayMusicTrackProjectionBatchResult(
    int ReplayedStreamCount,
    int ReplayedEventCount);
