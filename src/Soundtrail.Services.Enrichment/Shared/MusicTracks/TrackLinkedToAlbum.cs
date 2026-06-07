using Soundtrail.Services.Enrichment.Shared.Execution;

namespace Soundtrail.Services.Enrichment.Shared.MusicTracks;

public sealed record TrackLinkedToAlbum(
    string? AlbumId,
    string? AlbumTitle,
    ProviderName SourceProvider,
    DateTimeOffset ObservedAt) : MusicTrackFact(SourceProvider, ObservedAt);
