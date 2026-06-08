using Soundtrail.Domain.Events;

namespace Soundtrail.Domain.Model;

public sealed record MusicTrackStream(
    int Version,
    IReadOnlyList<MusicTrackFact> Facts);
