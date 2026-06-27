using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Tools.MusicBrainzImport.Features.ReplayDiscoveryLifecycleProjection.Adapters;
using Soundtrail.Tools.MusicBrainzImport.Features.ReplayDiscoveryLifecycleProjection.EventStore;
using Soundtrail.Tools.MusicBrainzImport.Features.ReplayDiscoveryLifecycleProjection.ProjectionReset;

namespace Soundtrail.Tools.MusicBrainzImport.Features.ReplayDiscoveryLifecycleProjection.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOnReplayCatalogSearchStatusFeature(this IServiceCollection services)
    {
        services.TryAddScoped<ReplayDiscoveryLifecycleProjectionBatchHandler>();
        services.TryAddScoped<ILoadDiscoveryLifecycleReplayTargetsPort, RavenLoadDiscoveryLifecycleReplayTargets>();
        services.TryAddScoped<ILoadDiscoveryLifecycleEventsForReplayPort, RavenLoadDiscoveryLifecycleEventsForReplay>();
        services.TryAddScoped<IResetDiscoveryLifecycleProjectionPort, RavenResetDiscoveryLifecycleProjection>();
        return services;
    }
}
