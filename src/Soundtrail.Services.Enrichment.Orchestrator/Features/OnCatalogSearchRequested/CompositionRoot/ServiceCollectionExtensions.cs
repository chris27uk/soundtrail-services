using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnCatalogSearchRequested.Adapters;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Search.Resolution;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnCatalogSearchRequested.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOnCatalogSearchRequestedFeature(
        this IServiceCollection services,
        Action<OnCatalogSearchRequestedFeatureOptions>? configure = null)
    {
        var options = new OnCatalogSearchRequestedFeatureOptions();
        configure?.Invoke(options);
        options.ConfigureDependencies?.Invoke(services);

        services.TryAddSingleton<MusicCatalogMatchResolver>();
        services.TryAddScoped<ICatalogSearchDiscoveryRepository, RavenCatalogSearchDiscoveryRepository>();
        services.TryAddScoped<CatalogSearchRequestedHandler>();
        services.TryAddScoped<CatalogSearchRequestedListener>();
        return services;
    }
}
