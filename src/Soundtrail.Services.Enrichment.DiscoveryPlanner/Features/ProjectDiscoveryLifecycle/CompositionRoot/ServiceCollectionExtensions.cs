using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.ProjectDiscoveryLifecycle.Adapters;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.ProjectDiscoveryLifecycle.Support;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.ProjectDiscoveryLifecycle.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddProjectDiscoveryLifecycleFeature(this IServiceCollection services)
    {
        services.TryAddSingleton<DiscoveryLifecycleProjectionMutationService>();
        services.TryAddSingleton<DiscoveryLifecycleProjectionApplier>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, ProjectDiscoveryLifecycleSubscriptionHostedService>());
        return services;
    }
}
