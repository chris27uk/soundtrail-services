using Soundtrail.Domain.Events;

namespace Soundtrail.Domain.Model;

public sealed record VersionedMusicTrackEvent(
    int Version,
    IMusicTrackEvent Event);
