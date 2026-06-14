using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.CatalogBrowsing;
using Soundtrail.Domain.Search;

namespace Soundtrail.Services.Api.Infrastructure.CompositionRoot;

internal sealed class TestingNoOpCatalogSearchPort : ICatalogSearchPort
{
    public Task<LocalCatalogSearchResponse> SearchAsync(SearchCatalogCommand command, CancellationToken cancellationToken) =>
        Task.FromResult(new LocalCatalogSearchResponse([], null, IsComplete: true));
}

internal sealed class TestingNoOpCatalogReadPort : ICatalogReadPort
{
    public Task<ArtistDetailsResponse?> GetArtistAsync(ArtistId artistId, CancellationToken cancellationToken) =>
        Task.FromResult<ArtistDetailsResponse?>(null);

    public Task<IReadOnlyList<TrackSummary>> ListTracksByArtistAsync(ArtistId artistId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<TrackSummary>>([]);

    public Task<AlbumDetailsResponse?> GetAlbumAsync(ArtistId artistId, AlbumId albumId, CancellationToken cancellationToken) =>
        Task.FromResult<AlbumDetailsResponse?>(null);

    public Task<AlbumTracksResponse?> ListTracksByAlbumAsync(ArtistId artistId, AlbumId albumId, CancellationToken cancellationToken) =>
        Task.FromResult<AlbumTracksResponse?>(null);

    public Task<TrackDetailsResponse?> GetTrackAsync(ArtistId artistId, AlbumId albumId, TrackId trackId, CancellationToken cancellationToken) =>
        Task.FromResult<TrackDetailsResponse?>(null);
}
