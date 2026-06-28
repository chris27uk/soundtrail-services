using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnCatalogDiscoveryWorkChanged.Adapters;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnCatalogDiscoveryWorkChanged.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOnCatalogDiscoveryWorkChangedFeature(this IServiceCollection services)
    {
        services.TryAddScoped<CatalogDiscoveryWorkSummaryProjector>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, CatalogDiscoveryWorkChangedSubscriptionHostedService>());
        return services;
    }
}
