using Microsoft.AspNetCore.TestHost;
using Soundtrail.Adapters.Registry;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Services.Api.Features.Search.Adapters;
using Soundtrail.Services.Api.Features.Search.Contract;
using Soundtrail.Services.Api.Features.Search.Registrations;

namespace Soundtrail.Services.Tests.Integration.Api.Search;

internal sealed class SearchRouteTestEnvironment : IDisposable
{
    private readonly WebApplication app;

    private SearchRouteTestEnvironment(WebApplication app)
    {
        this.app = app;
    }

    public HttpClient Client => app.GetTestClient();

    public static SearchRouteTestEnvironment ForExistingSearchResults()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<IApiHandler<SearchRequest, SearchResponse?>>(new SearchHandlerFake());
        var app = builder.Build();
        app.MapSearchEndpoints(new TypeRegistryFake());
        app.StartAsync().GetAwaiter().GetResult();
        return new SearchRouteTestEnvironment(app);
    }

    public void Dispose()
    {
        app.StopAsync().GetAwaiter().GetResult();
        app.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    private sealed class SearchHandlerFake : IApiHandler<SearchRequest, SearchResponse?>
    {
        public Task<SearchResponse?> Handle(SearchRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<SearchResponse?>(
                new SearchResponse(
                    "u2",
                    SearchFilter.Artist,
                    [
                        new SearchResultResponse(
                            new CatalogItemId.Artist(ArtistId.From("artist-3001")),
                            SearchFilter.Artist,
                            "U2",
                            null,
                            null,
                            "https://cdn.soundtrail.test/artists/artist-3001.jpg")
                    ]));
    }

    private sealed class TypeRegistryFake : ITypeRegistry
    {
        public TDto ToDto<TDto>(object domainObject) where TDto : class => (ToDto(domainObject) as TDto)!;

        public object ToDto(object domainObject)
        {
            var response = (SearchResponse)domainObject;
            return new SearchResponseDto(
                response.QueryText,
                response.Filter.ToString(),
                response.Results.Select(
                        result => new SearchResultResponseDto(
                            result.MusicCatalogId.NormalisedIdentifier,
                            result.ResultType.ToString(),
                            result.Title,
                            result.ArtistName,
                            result.AlbumTitle,
                            result.ArtworkUrl))
                    .ToArray());
        }

        public TDomain ToDomainObject<TDomain>(object dto) where TDomain : class => throw new NotSupportedException();

        public object ToDomainObject(object? dto) => throw new NotSupportedException();

        public void MapOnto<TSource, TTarget>(TSource source, TTarget target)
            where TSource : class
            where TTarget : class => throw new NotSupportedException();
    }
}
