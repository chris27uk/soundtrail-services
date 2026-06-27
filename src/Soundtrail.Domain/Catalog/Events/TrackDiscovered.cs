using Soundtrail.Contracts.Common;

namespace Soundtrail.Domain.Catalog.Events;

public sealed record TrackDiscovered(
    string Title,
    string Artist,
    int? DurationMs,
    string? Isrc,
    string? Mbid,
    ProviderName SourceProvider,
    DateTimeOffset ObservedAt) : IMusicTrackEvent;
