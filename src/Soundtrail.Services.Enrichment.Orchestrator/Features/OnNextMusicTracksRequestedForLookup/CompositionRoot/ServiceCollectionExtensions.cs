using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnNextMusicTracksRequestedForLookup.Adapters;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnNextMusicTracksRequestedForLookup.Support;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnNextMusicTracksRequestedForLookup.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOnNextMusicTracksRequestedForLookupFeature(
        this IServiceCollection services,
        Action<OnNextMusicTracksRequestedForLookupFeatureOptions>? configure = null)
    {
        var options = new OnNextMusicTracksRequestedForLookupFeatureOptions();
        configure?.Invoke(options);
        options.ConfigureDependencies?.Invoke(services);

        services.TryAddScoped<NextMusicTracksRequestedForLookupHandler>();
        services.TryAddScoped<DiscoveryBacklogLookupPlanner>();
        services.TryAddScoped<NextMusicTracksRequestedForLookupListener>();

        return services;
    }
}
