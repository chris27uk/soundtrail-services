using Microsoft.AspNetCore.TestHost;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Api.Features.Catalog.Search.Adapters;
using Soundtrail.Services.Api.Features.Catalog.Search.Contract;
using Soundtrail.Services.Api.Infrastructure;

namespace Soundtrail.Services.Tests.Integration.Features.Search;

internal sealed class SearchRouteTestEnvironment : IDisposable
{
    private readonly WebApplication app;

    private SearchRouteTestEnvironment(WebApplication app)
    {
        this.app = app;
    }

    public HttpClient Client => this.app.GetTestClient();

    public static SearchRouteTestEnvironment ForExistingSearchResults()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<IApiHandler<SearchRequest, SearchResponse?>>(new SearchHandlerFake());
        var app = builder.Build();
        app.MapSearchEndpoints(AppTypeRegistry.ServiceLocation);
        app.StartAsync().GetAwaiter().GetResult();
        return new SearchRouteTestEnvironment(app);
    }

    public void Dispose()
    {
        this.app.StopAsync().GetAwaiter().GetResult();
        this.app.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    private sealed class SearchHandlerFake : IApiHandler<SearchRequest, SearchResponse?>
    {
        public Task<SearchResponse?> Handle(SearchRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<SearchResponse?>(
                new SearchResponse(
                    "u2",
                    SearchType.Artist,
                    [
                        new SearchResultResponse(
                            new CatalogItemId.Artist(ArtistId.From("artist-3001")),
                            SearchType.Artist,
                            "U2",
                            null,
                            null,
                            "https://cdn.soundtrail.test/artists/artist-3001.jpg")
                    ]));
    }
}
