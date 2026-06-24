using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchStatusChanged.Adapters;

namespace Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchStatusChanged.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddProjectDiscoveryLifecycleFeature(this IServiceCollection services)
    {
        services.TryAddScoped<ProjectDiscoveryLifecycleHandler>();
        services.TryAddSingleton<RavenDiscoveryLifecycleProjectionMapper>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, ProjectDiscoveryLifecycleSubscriptionHostedService>());
        return services;
    }
}
