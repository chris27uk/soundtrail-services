using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownAlbumRequested.CompositionRoot;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownArtistRequested.CompositionRoot;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnCatalogSearchRequested.Adapters;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownCatalogItemRequested.Adapters;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownTrackRequested.CompositionRoot;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownCatalogItemRequested.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOnKnownCatalogItemRequestedFeature(this IServiceCollection services)
    {
        services.TryAddScoped<ICatalogSearchDiscoveryRepository, RavenCatalogSearchDiscoveryRepository>();
        services.AddOnKnownArtistRequestedFeature();
        services.AddOnKnownAlbumRequestedFeature();
        services.AddOnKnownTrackRequestedFeature();
        services.TryAddScoped<KnownCatalogItemRequestedListener>();
        return services;
    }
}
