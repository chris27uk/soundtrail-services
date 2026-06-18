namespace Soundtrail.Domain.Responses;

public sealed record ImportMusicTrackEventsResult(
    bool Appended,
    int ImportedEventCount);
