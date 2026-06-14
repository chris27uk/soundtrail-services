using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Services.Api;
using Soundtrail.Domain.CatalogBrowsing;

namespace Soundtrail.Services.Tests.Integration.Api.Features.Catalog;

public sealed class CatalogRoutesApiFactory : WebApplicationFactory<ApiAssemblyMarker>
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
