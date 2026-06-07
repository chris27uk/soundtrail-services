using Soundtrail.Services.Enrichment.Shared.Execution;

namespace Soundtrail.Services.Enrichment.Shared.MusicTracks;

public sealed record MinimalTrackInfoDiscovered(
    string Title,
    string Artist,
    int? DurationMs,
    string? Isrc,
    string? Mbid,
    ProviderName SourceProvider,
    DateTimeOffset ObservedAt) : MusicTrackFact(SourceProvider, ObservedAt);
