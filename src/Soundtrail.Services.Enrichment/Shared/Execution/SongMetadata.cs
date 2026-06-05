namespace Soundtrail.Services.Enrichment.Shared.Execution;

public sealed record SongMetadata(
    string Title,
    string Artist,
    string? Isrc,
    string? Mbid,
    int? DurationMs);
