using Soundtrail.Domain.Catalog.Events;

namespace Soundtrail.Domain.Catalog.Projection;

public sealed record VersionedMusicTrackEvent(
    int Version,
    IMusicTrackEvent Event);
