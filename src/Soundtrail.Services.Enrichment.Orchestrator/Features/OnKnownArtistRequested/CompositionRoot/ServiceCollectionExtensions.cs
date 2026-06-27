using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownArtistRequested.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOnKnownArtistRequestedFeature(this IServiceCollection services)
    {
        services.TryAddScoped<KnownArtistRequestedHandler>();
        return services;
    }
}
