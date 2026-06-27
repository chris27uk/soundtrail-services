using Soundtrail.Contracts.Common;

namespace Soundtrail.Domain.Catalog.Events;

public sealed record AlbumDiscovered(
    string? AlbumId,
    string? AlbumTitle,
    string? SourceAlbumId,
    DateOnly? ReleaseDate,
    LookupSource SourceProvider,
    DateTimeOffset ObservedAt) : IMusicTrackEvent;
