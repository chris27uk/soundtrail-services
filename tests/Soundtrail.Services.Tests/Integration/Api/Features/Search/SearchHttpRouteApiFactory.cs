using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Contracts;
using Soundtrail.Services.Api.Features.Search.TrackSearch;
using Soundtrail.Services.Tests.Integration.Api.Infrastructure;

namespace Soundtrail.Services.Tests.Integration.Api.Features.Search;

public sealed class SearchHttpRouteApiFactory : WebApplicationFactory<Program>
{
    public ApiFakeSearchMusicHandler SearchMusicHandler { get; } = new();

    public static SearchHttpRouteApiFactory WithResolvedSearch(string query, params SearchResult[] results)
    {
        var factory = new SearchHttpRouteApiFactory();
        factory.SearchMusicHandler.RespondWith(
            SearchMusicResponse.Resolved(
                query,
                results));
        return factory;
    }

    public static SearchHttpRouteApiFactory WithPendingSearch(string query)
    {
        var factory = new SearchHttpRouteApiFactory();
        factory.SearchMusicHandler.RespondWith(
            SearchMusicResponse.Pending(
                query));
        return factory;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IHandler<SearchMusicRequest, SearchMusicResponse>>();
            services.AddSingleton<IHandler<SearchMusicRequest, SearchMusicResponse>>(SearchMusicHandler);
        });
    }
}
