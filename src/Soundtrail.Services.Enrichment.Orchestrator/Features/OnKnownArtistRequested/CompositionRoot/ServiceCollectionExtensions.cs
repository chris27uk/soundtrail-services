using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownArtistRequested.Adapters;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownArtistRequested.Ports;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownArtistRequested.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOnKnownArtistRequestedFeature(this IServiceCollection services)
    {
        services.TryAddScoped<ILoadKnownCatalogArtistPort, RavenLoadKnownCatalogArtistPort>();
        services.TryAddScoped<KnownArtistRequestedHandler>();
        services.TryAddScoped<DispatchKnownArtistLookupCommandHandler>();
        services.TryAddScoped<KnownArtistRequestedListener>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, ArtistCatalogLookupRequestedSubscriptionHostedService>());
        return services;
    }
}
