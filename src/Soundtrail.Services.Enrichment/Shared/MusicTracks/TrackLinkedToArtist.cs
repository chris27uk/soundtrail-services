using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Execution;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.MusicTracks;

public sealed record TrackLinkedToArtist(
    string? ArtistId,
    string? ArtistName,
    ProviderName SourceProvider,
    DateTimeOffset ObservedAt) : MusicTrackFact(SourceProvider, ObservedAt);
