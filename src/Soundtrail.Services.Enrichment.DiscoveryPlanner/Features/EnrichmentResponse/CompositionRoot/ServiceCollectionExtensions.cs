using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.EnrichmentResponse.Adapters;

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
        services.TryAddScoped<EnrichmentResponseListener>();
        return services;
    }
}
