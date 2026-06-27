using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Search;

namespace Soundtrail.Domain.Commands;

public sealed record KnownCatalogItemRequested(
    KnownCatalogItem KnownItem,
    PlaybackProviderFilter Playback,
    int TrustLevel,
    int RiskScore,
    DateTimeOffset OccurredAt,
    CorrelationId CorrelationId);

public sealed record KnownCatalogItem
{
    private KnownCatalogItem(ArtistId? artistId, AlbumId? albumId, TrackId? trackId)
    {
        var count =
            (artistId is null ? 0 : 1) +
            (albumId is null ? 0 : 1) +
            (trackId is null ? 0 : 1);

        if (count != 1)
        {
            throw new ArgumentException("A known catalog item must specify exactly one identity.");
        }

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
