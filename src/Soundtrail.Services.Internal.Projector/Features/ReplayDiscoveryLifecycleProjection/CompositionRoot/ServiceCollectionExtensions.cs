using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Services.Internal.Projector.Features.ReplayDiscoveryLifecycleProjection.Adapters;
using Soundtrail.Services.Internal.Projector.Features.ReplayDiscoveryLifecycleProjection.StoredEvents;

namespace Soundtrail.Services.Internal.Projector.Features.ReplayDiscoveryLifecycleProjection.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddReplayDiscoveryLifecycleProjectionFeature(this IServiceCollection services)
    {
        services.TryAddScoped<ILoadStoredDiscoveryLifecycleEventsPort, RavenLoadStoredDiscoveryLifecycleEvents>();
        services.TryAddScoped<ReplayDiscoveryLifecycleProjectionHandler>();
        return services;
    }
}
