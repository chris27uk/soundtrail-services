using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownAlbumDiscoveryCompleted.Adapters;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownAlbumDiscoveryCompleted.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOnKnownAlbumDiscoveryCompletedFeature(this IServiceCollection services)
    {
        services.TryAddScoped<ApplyKnownAlbumDiscoveryCompletedToArtistCatalogHandler>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, KnownAlbumDiscoveryCompletedSubscriptionHostedService>());
        return services;
    }
}
