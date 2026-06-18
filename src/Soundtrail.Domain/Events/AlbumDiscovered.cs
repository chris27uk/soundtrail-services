using Soundtrail.Contracts.Common;

namespace Soundtrail.Domain.Events;

public sealed record AlbumDiscovered(
    string? AlbumId,
    string? AlbumTitle,
    ProviderName SourceProvider,
    DateTimeOffset ObservedAt) : IMusicTrackEvent;
