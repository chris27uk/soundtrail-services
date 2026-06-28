using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnMusicCatalogLookupAttempted.Adapters;
using ICommandBus = Soundtrail.Domain.Abstractions.ICommandBus;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnMusicCatalogLookupAttempted.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOnMusicCatalogLookupAttemptedFeature(this IServiceCollection services)
    {
        services.TryAddScoped<RavenMusicCatalogMetadataFetchedCatalogSearchDiscoveryRepository>();
        services.TryAddScoped<ApplyMusicCatalogLookupAttemptedToCatalogHandler>();
        services.TryAddScoped<ApplyMusicCatalogLookupAttemptedToDiscoveryHandler>();
        services.TryAddScoped<MusicCatalogLookupAttemptedHandler>(sp => new MusicCatalogLookupAttemptedHandler(
            sp.GetRequiredService<ICommandBus>()));
        services.TryAddScoped<MusicCatalogLookupAttemptedListener>();
        services.TryAddScoped<ApplyMusicCatalogLookupAttemptedToCatalogListener>();
        services.TryAddScoped<ApplyMusicCatalogLookupAttemptedToDiscoveryListener>();
        return services;
    }
}
