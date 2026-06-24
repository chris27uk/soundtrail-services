using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnMusicCatalogLookupAttempted.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOnMusicCatalogLookupAttemptedFeature(this IServiceCollection services)
    {
        services.TryAddScoped<MusicCatalogLookupAttemptedHandler>();
        services.TryAddScoped<Adapters.MusicCatalogLookupAttemptedListener>();
        services.TryAddScoped<Soundtrail.Services.Enrichment.Orchestrator.Features.OnMusicCatalogLookupAttempted.Adapters.RavenMusicCatalogMetadataFetchedCatalogSearchDiscoveryRepository>();
        services.TryAddScoped<Soundtrail.Services.Enrichment.Orchestrator.Features.OnMusicCatalogLookupAttempted.Support.ICompleteTrackedDiscoveriesRepository>(
            sp => sp.GetRequiredService<Soundtrail.Services.Enrichment.Orchestrator.Features.OnMusicCatalogLookupAttempted.Adapters.RavenMusicCatalogMetadataFetchedCatalogSearchDiscoveryRepository>());
        return services;
    }
}
