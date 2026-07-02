using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnMusicCatalogLookupHistoryChanged.Adapters;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnMusicCatalogLookupHistoryChanged.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOnMusicCatalogLookupHistoryChangedFeature(this IServiceCollection services)
    {
        services.TryAddScoped<ApplyMusicCatalogLookupHistoryChangedToKnownTrackDiscoveryHandler>();
        services.TryAddScoped<ApplyMusicCatalogLookupHistoryChangedToSearchDiscoveryHandler>();
        services.TryAddScoped<ApplyMusicCatalogLookupHistoryChangedToCatalogHandler>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, MusicCatalogLookupHistoryToCatalogSubscriptionHostedService>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, MusicCatalogLookupHistoryToKnownTrackDiscoverySubscriptionHostedService>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, MusicCatalogLookupHistoryToSearchDiscoverySubscriptionHostedService>());
        return services;
    }
}
