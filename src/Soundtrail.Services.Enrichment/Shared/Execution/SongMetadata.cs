namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Execution;

public sealed record SongMetadata(
    string Title,
    string Artist,
    string? Isrc,
    string? Mbid,
    int? DurationMs);
