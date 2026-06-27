using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Projection;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnMusicCatalogLookupAttempted.Adapters;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnMusicCatalogLookupAttempted.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOnMusicCatalogLookupAttemptedFeature(this IServiceCollection services)
    {
        services.TryAddScoped<RavenMusicCatalogMetadataFetchedCatalogSearchDiscoveryRepository>();
        services.TryAddScoped<MusicCatalogLookupAttemptedHandler>(sp => new MusicCatalogLookupAttemptedHandler(
            sp.GetRequiredService<IMusicTrackEventRepository>(),
            sp.GetRequiredService<ICatalogSearchTrackingStore>(),
            sp.GetRequiredService<RavenMusicCatalogMetadataFetchedCatalogSearchDiscoveryRepository>()));
        services.TryAddScoped<MusicCatalogLookupAttemptedListener>();
        return services;
    }
}
