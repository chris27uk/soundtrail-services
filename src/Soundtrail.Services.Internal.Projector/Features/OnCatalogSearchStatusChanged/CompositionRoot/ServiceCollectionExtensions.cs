using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchStatusChanged.Adapters;

namespace Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchStatusChanged.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOnCatalogSearchStatusChangedFeature(this IServiceCollection services)
    {
        services.TryAddScoped<CatalogSearchStatusChangedHandler>();
        services.TryAddSingleton<RavenDiscoveryLifecycleProjectionMapper>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, CatalogSearchStatusChangedSubscriptionHostedService>());
        return services;
    }
}
