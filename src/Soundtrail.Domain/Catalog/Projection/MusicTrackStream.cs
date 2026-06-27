using Soundtrail.Domain.Catalog.Events;

namespace Soundtrail.Domain.Catalog.Projection;

public sealed record MusicTrackStream(int Version, IReadOnlyList<IMusicTrackEvent> Events);
