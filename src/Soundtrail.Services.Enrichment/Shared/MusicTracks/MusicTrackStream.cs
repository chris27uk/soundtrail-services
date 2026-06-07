namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.MusicTracks;

public sealed record MusicTrackStream(
    int Version,
    IReadOnlyList<MusicTrackFact> Facts);
