using Soundtrail.Services.Enrichment.Shared.Execution;

namespace Soundtrail.Services.Enrichment.Shared.MusicTracks;

public sealed record TrackLinkedToArtist(
    string? ArtistId,
    string? ArtistName,
    ProviderName SourceProvider,
    DateTimeOffset ObservedAt) : MusicTrackFact(SourceProvider, ObservedAt);
