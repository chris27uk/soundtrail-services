using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Model;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.LocalSearch;

public sealed record LocalMusicTrackSearchResult(
    MusicCatalogId MusicCatalogId,
    string? Title,
    string? Artist,
    string? AlbumTitle,
    string? Isrc,
    string? Mbid,
    int? DurationMs,
    bool IsPlayable)
{
    public bool HasIsrc() => !string.IsNullOrWhiteSpace(Isrc);

    public bool HasTrackNameAndArtist() =>
        !string.IsNullOrWhiteSpace(Title)
        && !string.IsNullOrWhiteSpace(Artist);

    public PlaybackReferenceLookupKey? ToPlaybackLookupKey() =>
        HasIsrc()
            ? PlaybackReferenceLookupKey.ByIsrc(Isrc!)
            : HasTrackNameAndArtist()
                ? PlaybackReferenceLookupKey.ByTrackNameAndArtist(Title!, Artist!)
                : null;

    public CanonicalMusicMetadataLookup? ToCanonicalMusicMetadataLookup() =>
        HasIsrc()
            ? CanonicalMusicMetadataLookup.FromIsrc(Isrc!)
            : HasTrackNameAndArtist()
                ? CanonicalMusicMetadataLookup.FromTrackNameArtistAndAlbum(Title!, Artist!, AlbumTitle)
                : null;
}
