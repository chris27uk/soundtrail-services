using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain;
using Soundtrail.Domain.CatalogBrowsing;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Api;
using Soundtrail.Services.Api.Infrastructure.Raven;
using Soundtrail.Services.Tests.Integration.Api.Features.Search;

namespace Soundtrail.Services.Tests.Integration.Api.WebApi.Search;

internal sealed class SearchWebApiTestEnvironment : IAsyncDisposable
{
    private readonly SearchWebApplicationFactory factory;

    private SearchWebApiTestEnvironment(SearchWebApplicationFactory factory)
    {
        this.factory = factory;
        Client = factory.CreateClient();
        SearchHandler = factory.SearchHandler;
    }

    public HttpClient Client { get; }

    public ApiFakeSearchCatalogHandler SearchHandler { get; }

    public static SearchWebApiTestEnvironment Create() => new(new SearchWebApplicationFactory());

    public ValueTask DisposeAsync()
    {
        Client.Dispose();
        factory.Dispose();
        return ValueTask.CompletedTask;
    }

    private sealed class SearchWebApplicationFactory : WebApplicationFactory<ApiAssemblyMarker>
    {
        public ApiFakeSearchCatalogHandler SearchHandler { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                foreach (var descriptor in services
                             .Where(x => x.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService)
                                         && x.ImplementationType == typeof(RavenDatabaseHostedService))
                             .ToArray())
                {
                    services.Remove(descriptor);
                }

                services.RemoveAll<IApiHandler<SearchCatalogCommand, SearchCatalogResponse>>();
                services.AddSingleton<IApiHandler<SearchCatalogCommand, SearchCatalogResponse>>(SearchHandler);
                services.RemoveAll<ICatalogReadPort>();
                services.AddSingleton<ICatalogReadPort, NoOpCatalogReadPort>();
            });
        }

        private sealed class NoOpCatalogReadPort : ICatalogReadPort
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
    }
}
