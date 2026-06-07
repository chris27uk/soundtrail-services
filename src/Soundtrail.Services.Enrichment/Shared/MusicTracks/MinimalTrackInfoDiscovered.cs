using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Execution;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.MusicTracks;

public sealed record MinimalTrackInfoDiscovered(
    string Title,
    string Artist,
    int? DurationMs,
    string? Isrc,
    string? Mbid,
    ProviderName SourceProvider,
    DateTimeOffset ObservedAt) : MusicTrackFact(SourceProvider, ObservedAt);
