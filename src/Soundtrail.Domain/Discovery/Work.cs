using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Playlists;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Search;

namespace Soundtrail.Domain.Discovery;

public static class Work
{
    public static EnrichmentTarget DiscoverArtistAlbums(ArtistId artistId) =>
        new EnrichmentTarget.KnownCatalogItemOperation(new CatalogItemOperation.ChildAlbumsForArtist(artistId));

    public static EnrichmentTarget DiscoverArtistTracks(ArtistId artistId) =>
        new EnrichmentTarget.KnownCatalogItemOperation(new CatalogItemOperation.ChildTracksForArtist(artistId));

    public static EnrichmentTarget DiscoverAlbumTracks(AlbumId albumId) =>
        new EnrichmentTarget.KnownCatalogItemOperation(new CatalogItemOperation.ChildTracksForAlbum(albumId));

    public static EnrichmentTarget DiscoverPlaylistTracks(PlaylistId playlistId) =>
        new EnrichmentTarget.KnownCatalogItemOperation(new CatalogItemOperation.ChildTracksForPlaylist(playlistId));

    public static EnrichmentTarget EnrichTrackStreamingLocation(TrackId trackId) =>
        new EnrichmentTarget.KnownCatalogItemOperation(new CatalogItemOperation.StreamingLocationForTrack(trackId));

    public static EnrichmentTarget SearchExternally(SearchCriteria searchCriteria) =>
        new EnrichmentTarget.SearchForUnknownCatalogItem(searchCriteria);
}
