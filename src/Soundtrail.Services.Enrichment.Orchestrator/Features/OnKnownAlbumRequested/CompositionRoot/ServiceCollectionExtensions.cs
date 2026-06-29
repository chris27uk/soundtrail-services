using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownAlbumRequested.Adapters;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownAlbumRequested.Ports;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownAlbumRequested.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOnKnownAlbumRequestedFeature(this IServiceCollection services)
    {
        services.TryAddScoped<ILoadKnownCatalogAlbumPort, RavenLoadKnownCatalogAlbumPort>();
        services.TryAddScoped<KnownAlbumRequestedHandler>();
        services.TryAddScoped<KnownAlbumRequestedListener>();
        return services;
    }
}
