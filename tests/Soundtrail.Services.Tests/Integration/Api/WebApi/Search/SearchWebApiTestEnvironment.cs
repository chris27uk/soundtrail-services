using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Domain;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Api;
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

    public ApiFakeSearchMusicHandler SearchHandler { get; }

    public static SearchWebApiTestEnvironment Create() => new(new SearchWebApplicationFactory());

    public ValueTask DisposeAsync()
    {
        Client.Dispose();
        factory.Dispose();
        return ValueTask.CompletedTask;
    }

    private sealed class SearchWebApplicationFactory : WebApplicationFactory<ApiAssemblyMarker>
    {
        public ApiFakeSearchMusicHandler SearchHandler { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IHandler<SearchCatalogCommand, SearchCatalogResponse>>();
                services.AddSingleton<IHandler<SearchCatalogCommand, SearchCatalogResponse>>(SearchHandler);
            });
        }
    }
}
