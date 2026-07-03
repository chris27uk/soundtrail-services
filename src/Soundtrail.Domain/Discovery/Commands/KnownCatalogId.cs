using Soundtrail.Domain.Catalog;

namespace Soundtrail.Domain.Discovery.Commands
{
    public sealed record KnownCatalogId
    {
        private KnownCatalogId(ArtistId? artistId, AlbumId? albumId, TrackId? trackId)
        {
            ArtistId = artistId;
            AlbumId = albumId;
            TrackId = trackId;
        }

        public ArtistId? ArtistId { get; }

        public AlbumId? AlbumId { get; }

        public TrackId? TrackId { get; }

        public static KnownCatalogId ForArtist(ArtistId artistId) => new(artistId, null, null);

        public static KnownCatalogId ForAlbum(ArtistId artistId, AlbumId albumId) => new(artistId, albumId, null);

        public static KnownCatalogId ForTrack(TrackId trackId) => new(null, null, trackId);
    }
}
