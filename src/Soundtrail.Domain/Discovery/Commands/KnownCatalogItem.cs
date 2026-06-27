using Soundtrail.Domain.Catalog;

namespace Soundtrail.Domain.Discovery.Commands
{
    public sealed record KnownCatalogItem
    {
        private KnownCatalogItem(ArtistId? artistId, AlbumId? albumId, TrackId? trackId)
        {
            ArtistId = artistId;
            AlbumId = albumId;
            TrackId = trackId;
        }

        public ArtistId? ArtistId { get; }

        public AlbumId? AlbumId { get; }

        public TrackId? TrackId { get; }

        public static KnownCatalogItem ForArtist(ArtistId artistId) => new(artistId, null, null);

        public static KnownCatalogItem ForAlbum(AlbumId albumId) => new(null, albumId, null);

        public static KnownCatalogItem ForTrack(TrackId trackId) => new(null, null, trackId);
    }
}