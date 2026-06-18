using Soundtrail.Domain.Catalog;

namespace Soundtrail.Domain.CatalogBrowsing;

public interface ICatalogReadPort
{
    Task<ArtistDetailsResponse?> GetArtistAsync(ArtistId artistId, CancellationToken cancellationToken);

    Task<IReadOnlyList<TrackSummary>> ListTracksByArtistAsync(ArtistId artistId, CancellationToken cancellationToken);

    Task<AlbumDetailsResponse?> GetAlbumAsync(ArtistId artistId, AlbumId albumId, CancellationToken cancellationToken);

    Task<AlbumTracksResponse?> ListTracksByAlbumAsync(ArtistId artistId, AlbumId albumId, CancellationToken cancellationToken);

    Task<TrackDetailsResponse?> GetTrackAsync(ArtistId artistId, AlbumId albumId, TrackId trackId, CancellationToken cancellationToken);
}
