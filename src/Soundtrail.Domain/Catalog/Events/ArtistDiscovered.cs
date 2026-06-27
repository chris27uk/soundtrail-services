using Soundtrail.Contracts.Common;

namespace Soundtrail.Domain.Catalog.Events;

public sealed record ArtistDiscovered(
    string? ArtistId,
    string? ArtistName,
    string? SourceArtistId,
    LookupSource SourceProvider,
    DateTimeOffset ObservedAt) : IMusicTrackEvent;
