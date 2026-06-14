using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
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
        services.TryAddSingleton<MusicTrackProjectionApplier>();

        if (options.IncludeProjectionHostedService)
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, MusicTrackProjectionSubscriptionHostedService>());
        }

        return services;
    }
}
