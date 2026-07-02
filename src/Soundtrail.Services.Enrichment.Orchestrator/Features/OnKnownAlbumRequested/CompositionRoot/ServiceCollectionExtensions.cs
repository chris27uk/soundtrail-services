using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownAlbumRequested.Adapters;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownAlbumRequested.Ports;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownAlbumRequested.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOnKnownAlbumRequestedFeature(this IServiceCollection services)
    {
        services.TryAddScoped<ILoadKnownCatalogAlbumPort, RavenLoadKnownCatalogAlbumPort>();
        services.TryAddScoped<KnownAlbumRequestedHandler>();
        services.TryAddScoped<DispatchKnownAlbumLookupCommandHandler>();
        services.TryAddScoped<KnownAlbumRequestedListener>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, AlbumCatalogLookupRequestedSubscriptionHostedService>());
        return services;
    }
}
