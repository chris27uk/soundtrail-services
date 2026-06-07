namespace Soundtrail.Services.Enrichment.Shared.MusicTracks;

public sealed record AppendMusicTrackStreamResult(
    bool Appended,
    int Version,
    IReadOnlyList<MusicTrackFact> AppendedFacts);
