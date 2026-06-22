using Soundtrail.Contracts.Common;

namespace Soundtrail.Domain.Events;

public sealed record AlbumDiscovered(
    string? AlbumId,
    string? AlbumTitle,
    string? SourceAlbumId,
    DateOnly? ReleaseDate,
    ProviderName SourceProvider,
    DateTimeOffset ObservedAt) : IMusicTrackEvent;
