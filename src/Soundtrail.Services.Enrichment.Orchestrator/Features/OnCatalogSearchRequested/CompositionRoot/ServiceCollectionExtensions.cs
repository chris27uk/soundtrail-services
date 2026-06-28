using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnCatalogSearchRequested.Adapters;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Search.Resolution;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnCatalogSearchRequested.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOnSearchCatalogRequestedFeature(
        this IServiceCollection services,
        Action<OnSearchCatalogRequestedFeatureOptions>? configure = null)
    {
        var options = new OnSearchCatalogRequestedFeatureOptions();
        configure?.Invoke(options);
        options.ConfigureDependencies?.Invoke(services);

        services.TryAddScoped<MusicCatalogMatchResolver>();
        services.TryAddScoped<SearchCatalogRequestedHandler>();
        services.TryAddScoped<SearchCatalogRequestedListener>();
        return services;
    }
}
