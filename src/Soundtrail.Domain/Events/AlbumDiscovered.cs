using Soundtrail.Contracts.Common;

namespace Soundtrail.Domain.Events;

public sealed record AlbumDiscovered(
    string? AlbumId,
    string? AlbumTitle,
    DateOnly? ReleaseDate,
    ProviderName SourceProvider,
    DateTimeOffset ObservedAt) : IMusicTrackEvent;
