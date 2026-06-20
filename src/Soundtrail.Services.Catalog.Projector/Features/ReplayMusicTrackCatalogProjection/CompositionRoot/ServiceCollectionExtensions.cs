using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Services.Catalog.Projector.Features.ReplayMusicTrackCatalogProjection.Adapters;
using Soundtrail.Services.Catalog.Projector.Features.ReplayMusicTrackCatalogProjection.StoredEvents;

namespace Soundtrail.Services.Catalog.Projector.Features.ReplayMusicTrackCatalogProjection.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddReplayMusicTrackCatalogProjectionFeature(this IServiceCollection services)
    {
        services.TryAddScoped<ILoadStoredMusicTrackEventsPort, RavenLoadStoredMusicTrackEvents>();
        services.TryAddScoped<ReplayMusicTrackCatalogProjectionHandler>();
        return services;
    }
}
