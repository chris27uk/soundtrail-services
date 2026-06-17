using Soundtrail.Contracts.Common;

namespace Soundtrail.Domain.Events;

public sealed record MinimalTrackInfoDiscovered(
    string Title,
    string Artist,
    int? DurationMs,
    string? Isrc,
    string? Mbid,
    ProviderName SourceProvider,
    DateTimeOffset ObservedAt) : IMusicTrackEvent;
