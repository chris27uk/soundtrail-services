using Soundtrail.Domain.Events;

namespace Soundtrail.Domain.Model;

public sealed record AppendMusicTrackStreamResult(
    bool Appended,
    int Version,
    IReadOnlyList<MusicTrackFact> AppendedFacts);
