using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.ReplayMusicTrackProjection.Adapters;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.ReplayMusicTrackProjection.StoredEvents;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.ReplayMusicTrackProjection.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddReplayMusicTrackProjectionFeature(this IServiceCollection services)
    {
        services.TryAddScoped<ReplayMusicTrackProjectionHandler>();
        services.TryAddScoped<ILoadStoredMusicTrackEventsPort, RavenLoadStoredMusicTrackEvents>();
        return services;
    }
}
