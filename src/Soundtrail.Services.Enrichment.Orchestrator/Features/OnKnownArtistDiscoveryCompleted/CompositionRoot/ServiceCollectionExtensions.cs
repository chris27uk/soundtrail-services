using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownArtistDiscoveryCompleted.Adapters;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownArtistDiscoveryCompleted.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOnKnownArtistDiscoveryCompletedFeature(this IServiceCollection services)
    {
        services.TryAddScoped<ApplyKnownArtistDiscoveryCompletedToArtistCatalogHandler>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, KnownArtistDiscoveryCompletedSubscriptionHostedService>());
        return services;
    }
}
