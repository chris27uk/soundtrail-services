using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.CatalogBrowsing;

namespace Soundtrail.Services.Tests.Integration.Api.Ports.CatalogRead;

internal sealed class FakeCatalogReadPort : ICatalogReadPort
{
    private ArtistDetailsResponse? artist;
    private AlbumDetailsResponse? album;
    private TrackDetailsResponse? track;
    private IReadOnlyList<TrackSummary> artistTracks = [];
    private IReadOnlyList<TrackSummary> albumTracks = [];

    public void Seed(
        ArtistDetailsResponse? artist,
        AlbumDetailsResponse? album,
        TrackDetailsResponse? track,
        IReadOnlyList<TrackSummary>? artistTracks = null,
        IReadOnlyList<TrackSummary>? albumTracks = null)
    {
        this.artist = artist;
        this.album = album;
        this.track = track;
        this.artistTracks = artistTracks ?? [];
        this.albumTracks = albumTracks ?? [];
    }

    public Task<ArtistDetailsResponse?> GetArtistAsync(ArtistId artistId, CancellationToken cancellationToken) =>
        Task.FromResult(artist?.ArtistId == artistId ? artist : null);

    public Task<IReadOnlyList<TrackSummary>> ListTracksByArtistAsync(ArtistId artistId, CancellationToken cancellationToken) =>
        Task.FromResult(this.artistTracks);

    public Task<AlbumDetailsResponse?> GetAlbumAsync(ArtistId artistId, AlbumId albumId, CancellationToken cancellationToken) =>
        Task.FromResult(album?.ArtistId == artistId && album.AlbumId == albumId ? album : null);

    public Task<AlbumTracksResponse?> ListTracksByAlbumAsync(ArtistId artistId, AlbumId albumId, CancellationToken cancellationToken) =>
        Task.FromResult(
            album?.ArtistId == artistId && album.AlbumId == albumId
                ? new AlbumTracksResponse(artistId, album.ArtistName, albumId, album.Name, this.albumTracks)
                : null);

    public Task<TrackDetailsResponse?> GetTrackAsync(ArtistId artistId, AlbumId albumId, TrackId trackId, CancellationToken cancellationToken) =>
        Task.FromResult(track?.ArtistId == artistId && track.AlbumId == albumId && track.TrackId == trackId ? track : null);
}
