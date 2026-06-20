using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.ReplayDiscoveryLifecycleProjection.Adapters;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.ReplayDiscoveryLifecycleProjection.StoredEvents;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.ReplayDiscoveryLifecycleProjection.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddReplayDiscoveryLifecycleProjectionFeature(this IServiceCollection services)
    {
        services.TryAddScoped<ILoadStoredDiscoveryLifecycleEventsPort, RavenLoadStoredDiscoveryLifecycleEvents>();
        services.TryAddScoped<ReplayDiscoveryLifecycleProjectionHandler>();
        return services;
    }
}
