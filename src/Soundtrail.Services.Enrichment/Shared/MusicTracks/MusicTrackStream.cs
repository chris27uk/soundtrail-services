namespace Soundtrail.Services.Enrichment.Shared.MusicTracks;

public sealed record MusicTrackStream(
    int Version,
    IReadOnlyList<MusicTrackFact> Facts);
