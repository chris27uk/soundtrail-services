using Soundtrail.Contracts.Common;

namespace Soundtrail.Domain.Events;

public sealed record ArtistDiscovered(
    string? ArtistId,
    string? ArtistName,
    ProviderName SourceProvider,
    DateTimeOffset ObservedAt) : IMusicTrackEvent;
