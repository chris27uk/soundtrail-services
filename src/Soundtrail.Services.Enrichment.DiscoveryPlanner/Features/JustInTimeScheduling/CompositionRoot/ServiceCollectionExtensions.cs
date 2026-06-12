using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.Adapters;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Search.Resolution;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddJustInTimeSchedulingFeature(
        this IServiceCollection services,
        Action<JustInTimeSchedulingFeatureOptions>? configure = null)
    {
        var options = new JustInTimeSchedulingFeatureOptions();
        configure?.Invoke(options);
        options.ConfigureDependencies?.Invoke(services);

        services.TryAddSingleton<MusicCatalogMatchResolver>();
        services.TryAddScoped<LookupMusicRequestHandler>();
        services.TryAddScoped<LookupMusicRequestListener>();
        return services;
    }
}
