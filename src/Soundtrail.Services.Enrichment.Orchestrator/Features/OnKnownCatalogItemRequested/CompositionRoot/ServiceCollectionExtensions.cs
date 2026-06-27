using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnCatalogSearchRequested.Adapters;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownCatalogItemRequested.Adapters;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownCatalogItemRequested.Ports;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownCatalogItemRequested.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOnKnownCatalogItemRequestedFeature(this IServiceCollection services)
    {
        services.TryAddScoped<ICatalogSearchDiscoveryRepository, RavenCatalogSearchDiscoveryRepository>();
        services.TryAddScoped<ILoadKnownCatalogTrackPort, RavenLoadKnownCatalogTrackPort>();
        services.TryAddScoped<KnownCatalogItemRequestedHandler>();
        services.TryAddScoped<KnownCatalogItemRequestedListener>();
        return services;
    }
}
