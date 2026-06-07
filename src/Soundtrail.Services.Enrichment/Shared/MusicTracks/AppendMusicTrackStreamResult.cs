namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.MusicTracks;

public sealed record AppendMusicTrackStreamResult(
    bool Appended,
    int Version,
    IReadOnlyList<MusicTrackFact> AppendedFacts);
