using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Services.Internal.Projector.Features.OnReplayCatalogSearchStatus.Adapters;
using Soundtrail.Services.Internal.Projector.Features.OnReplayCatalogSearchStatus.StoredEvents;

namespace Soundtrail.Services.Internal.Projector.Features.OnReplayCatalogSearchStatus.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOnReplayCatalogSearchStatusFeature(this IServiceCollection services)
    {
        services.TryAddScoped<ILoadStoredDiscoveryLifecycleEventsPort, RavenLoadStoredDiscoveryLifecycleEvents>();
        services.TryAddScoped<ReplayCatalogSearchStatusHandler>();
        return services;
    }
}
