using Soundtrail.Contracts.Common;

namespace Soundtrail.Domain.Catalog.Events;

public sealed record TrackDiscovered(
    MusicCatalogId? MusicCatalogId,
    string Title,
    string Artist,
    int? DurationMs,
    string? Isrc,
    string? Mbid,
    LookupSource SourceProvider,
    DateTimeOffset ObservedAt) : IMusicTrackEvent
{
    public TrackDiscovered(
        string Title,
        string Artist,
        int? DurationMs,
        string? Isrc,
        string? Mbid,
        LookupSource SourceProvider,
        DateTimeOffset ObservedAt)
        : this(null, Title, Artist, DurationMs, Isrc, Mbid, SourceProvider, ObservedAt)
    {
    }
}
