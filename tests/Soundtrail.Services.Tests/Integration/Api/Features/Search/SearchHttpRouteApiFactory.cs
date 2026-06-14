using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Contracts;
using Soundtrail.Domain;
using Soundtrail.Services.Api;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Tests.Integration.Api.Infrastructure;

namespace Soundtrail.Services.Tests.Integration.Api.Features.Search;

public sealed class SearchHttpRouteApiFactory : WebApplicationFactory<ApiAssemblyMarker>
{
    public ApiFakeSearchMusicHandler SearchMusicHandler { get; } = new();

    public static SearchHttpRouteApiFactory WithResolvedSearch(string query, params SearchCatalogResult[] results)
    {
        var factory = new SearchHttpRouteApiFactory();
        factory.SearchMusicHandler.RespondWith(
            new SearchCatalogResponse(
                query,
                results,
                new SearchDiscovery(false, null, null)));
        return factory;
    }

    public static SearchHttpRouteApiFactory WithPendingSearch(string query)
    {
        var factory = new SearchHttpRouteApiFactory();
        factory.SearchMusicHandler.RespondWith(
            new SearchCatalogResponse(
                query,
                [],
                new SearchDiscovery(true, "Local results incomplete", 60)));
        return factory;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IHandler<SearchCatalogCommand, SearchCatalogResponse>>();
            services.AddSingleton<IHandler<SearchCatalogCommand, SearchCatalogResponse>>(SearchMusicHandler);
        });
    }
}
