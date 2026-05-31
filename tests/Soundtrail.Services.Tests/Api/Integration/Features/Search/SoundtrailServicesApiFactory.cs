using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Services.Features.CatalogLookup.Contracts;
using Soundtrail.Services.Features.Search.Contracts;

namespace Soundtrail.Services.Tests.Integration.Features.Search;

public sealed class SoundtrailServicesApiFactory : WebApplicationFactory<Program>
{
    public ApiFakeQueryCachePort QueryCache { get; } = new();

    public ApiFakeCatalogLookupPort CatalogLookup { get; } = new();

    public ApiFakeTrackSearchPort TrackSearch { get; } = new();

    public ApiFakeResolutionDemandPort DemandStore { get; } = new();

    public ApiFakeResolutionDemandSignalPort DemandSignals { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IQueryCachePort>();
            services.RemoveAll<ICatalogLookupPort>();
            services.RemoveAll<ITrackSearchPort>();
            services.RemoveAll<IResolutionDemandPort>();
            services.RemoveAll<IResolutionDemandSignalPort>();

            services.AddSingleton<IQueryCachePort>(QueryCache);
            services.AddSingleton<ICatalogLookupPort>(CatalogLookup);
            services.AddSingleton<ITrackSearchPort>(TrackSearch);
            services.AddSingleton<IResolutionDemandPort>(DemandStore);
            services.AddSingleton<IResolutionDemandSignalPort>(DemandSignals);
        });
    }
}
