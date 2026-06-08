using Soundtrail.Contracts.Common;

namespace Soundtrail.Domain.Events;

public sealed record TrackLinkedToArtist(
    string? ArtistId,
    string? ArtistName,
    ProviderName SourceProvider,
    DateTimeOffset ObservedAt) : MusicTrackFact;
