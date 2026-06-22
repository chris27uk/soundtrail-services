using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Services.Enrichment.Orchestrator.Features.ReplayMusicTrackProjection.Adapters;
using Soundtrail.Services.Enrichment.Orchestrator.Features.ReplayMusicTrackProjection.StoredEvents;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.ReplayMusicTrackProjection.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddReplayMusicTrackProjectionFeature(this IServiceCollection services)
    {
        services.TryAddScoped<ReplayMusicTrackProjectionHandler>();
        services.TryAddScoped<ILoadStoredMusicTrackEventsPort, RavenLoadStoredMusicTrackEvents>();
        return services;
    }
}
