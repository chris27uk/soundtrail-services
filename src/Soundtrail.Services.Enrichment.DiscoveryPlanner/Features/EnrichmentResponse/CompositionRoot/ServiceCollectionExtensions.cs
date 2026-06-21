using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.EnrichmentResponse.Adapters;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.EnrichmentResponse.Support;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.EnrichmentResponse.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEnrichmentResponseFeature(
        this IServiceCollection services,
        Action<EnrichmentResponseFeatureOptions>? configure = null)
    {
        var options = new EnrichmentResponseFeatureOptions();
        configure?.Invoke(options);
        options.ConfigureDependencies?.Invoke(services);

        services.TryAddScoped<ApplyEnrichmentResponseHandler>();
        services.TryAddScoped<RavenEnrichmentResponseCatalogSearchDiscoveryRepository>();
        services.TryAddScoped<ICompleteTrackedDiscoveriesRepository>(sp => sp.GetRequiredService<RavenEnrichmentResponseCatalogSearchDiscoveryRepository>());
        services.TryAddScoped<EnrichmentResponseListener>();

        return services;
    }
}
