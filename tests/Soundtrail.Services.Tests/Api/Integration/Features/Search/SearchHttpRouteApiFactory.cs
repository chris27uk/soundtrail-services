using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Services.Features.Search;
using Soundtrail.Services.Features.Search.TrackSearch;
using Soundtrail.Services.Tests.Api.Integration.Infrastructure;

namespace Soundtrail.Services.Tests.Api.Integration.Features.Search;

public sealed class SearchHttpRouteApiFactory : WebApplicationFactory<Program>
{
    public ApiFakeSearchMusicHandler SearchMusicHandler { get; } = new();

    public static SearchHttpRouteApiFactory WithResolvedSearch(string query, params SearchResult[] results)
    {
        var factory = new SearchHttpRouteApiFactory();
        factory.SearchMusicHandler.RespondWith(
            SearchMusicResponse.Resolved(
                SearchQuery.From(query),
                results));
        return factory;
    }

    public static SearchHttpRouteApiFactory WithPendingSearch(string query)
    {
        var factory = new SearchHttpRouteApiFactory();
        factory.SearchMusicHandler.RespondWith(
            SearchMusicResponse.Pending(
                SearchQuery.From(query)));
        return factory;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<Shared.IHandler<SearchMusicRequest, SearchMusicResponse>>();
            services.AddSingleton<Shared.IHandler<SearchMusicRequest, SearchMusicResponse>>(SearchMusicHandler);
        });
    }
}
