using Soundtrail.Domain.Catalog.Events;

namespace Soundtrail.Domain.Catalog.Projection;

public sealed record AppendMusicTrackStreamResult(
    bool Appended,
    int Version,
    IReadOnlyList<IMusicTrackEvent> AppendedEvents);
