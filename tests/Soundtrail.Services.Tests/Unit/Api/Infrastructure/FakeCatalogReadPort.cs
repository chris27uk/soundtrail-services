using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.CatalogBrowsing;

namespace Soundtrail.Services.Tests.Unit.Api.Infrastructure;

internal sealed class FakeCatalogReadPort : ICatalogReadPort
{
    public ArtistDetailsResponse? Artist { get; set; }

    public AlbumDetailsResponse? Album { get; set; }

    public TrackDetailsResponse? Track { get; set; }

    public IReadOnlyList<TrackSummary> ArtistTracks { get; set; } = [];

    public IReadOnlyList<TrackSummary> AlbumTracks { get; set; } = [];

    public Task<ArtistDetailsResponse?> GetArtistAsync(ArtistId artistId, CancellationToken cancellationToken) =>
        Task.FromResult(Artist);

    public Task<IReadOnlyList<TrackSummary>> ListTracksByArtistAsync(ArtistId artistId, CancellationToken cancellationToken) =>
        Task.FromResult(ArtistTracks);

    public Task<AlbumDetailsResponse?> GetAlbumAsync(ArtistId artistId, AlbumId albumId, CancellationToken cancellationToken) =>
        Task.FromResult(Album?.ArtistId == artistId && Album.AlbumId == albumId ? Album : null);

    public Task<AlbumTracksResponse?> ListTracksByAlbumAsync(ArtistId artistId, AlbumId albumId, CancellationToken cancellationToken) =>
        Task.FromResult(
            Album?.ArtistId == artistId && Album.AlbumId == albumId
                ? new AlbumTracksResponse(artistId, Album.ArtistName, albumId, Album.Name, AlbumTracks)
                : null);

    public Task<TrackDetailsResponse?> GetTrackAsync(ArtistId artistId, AlbumId albumId, TrackId trackId, CancellationToken cancellationToken) =>
        Task.FromResult(Track?.ArtistId == artistId && Track.AlbumId == albumId && Track.TrackId == trackId ? Track : null);
}
