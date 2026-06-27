using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownAlbumRequested.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOnKnownAlbumRequestedFeature(this IServiceCollection services)
    {
        services.TryAddScoped<KnownAlbumRequestedHandler>();
        return services;
    }
}
