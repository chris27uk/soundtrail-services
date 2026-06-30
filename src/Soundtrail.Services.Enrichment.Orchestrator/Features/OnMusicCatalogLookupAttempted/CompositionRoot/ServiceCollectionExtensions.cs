using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnMusicCatalogLookupAttempted.Adapters;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnMusicCatalogLookupAttempted.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOnMusicCatalogLookupAttemptedFeature(this IServiceCollection services)
    {
        services.TryAddScoped<MusicCatalogLookupAttemptedHandler>();
        services.TryAddScoped<MusicCatalogLookupAttemptedListener>();
        return services;
    }
}
