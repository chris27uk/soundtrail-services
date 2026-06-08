using Soundtrail.Contracts.Common;

namespace Soundtrail.Domain.Events;

public sealed record TrackLinkedToAlbum(
    string? AlbumId,
    string? AlbumTitle,
    ProviderName SourceProvider,
    DateTimeOffset ObservedAt) : MusicTrackFact;
