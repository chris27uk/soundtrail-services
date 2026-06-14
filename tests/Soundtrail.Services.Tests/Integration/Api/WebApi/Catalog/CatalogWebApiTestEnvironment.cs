using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Domain.CatalogBrowsing;
using Soundtrail.Services.Api;
using Soundtrail.Services.Tests.Integration.Api.Features.Catalog;

namespace Soundtrail.Services.Tests.Integration.Api.WebApi.Catalog;

internal sealed class CatalogWebApiTestEnvironment : IAsyncDisposable
{
    private readonly CatalogWebApplicationFactory factory;

    private CatalogWebApiTestEnvironment(CatalogWebApplicationFactory factory)
    {
        this.factory = factory;
        Client = factory.CreateClient();
        CatalogReadPort = factory.CatalogReadPort;
    }

    public HttpClient Client { get; }

    public FakeCatalogReadPort CatalogReadPort { get; }

    public static CatalogWebApiTestEnvironment Create() => new(new CatalogWebApplicationFactory());

    public ValueTask DisposeAsync()
    {
        Client.Dispose();
        factory.Dispose();
        return ValueTask.CompletedTask;
    }

    private sealed class CatalogWebApplicationFactory : WebApplicationFactory<ApiAssemblyMarker>
    {
        public FakeCatalogReadPort CatalogReadPort { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<ICatalogReadPort>();
                services.AddSingleton<ICatalogReadPort>(CatalogReadPort);
            });
        }
    }
}
