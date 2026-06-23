using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Services.Internal.Projector.Features.ReplayMusicTrackProjection.Adapters;
using Soundtrail.Services.Internal.Projector.Features.ReplayMusicTrackProjection.StoredEvents;

namespace Soundtrail.Services.Internal.Projector.Features.ReplayMusicTrackProjection.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddReplayMusicTrackProjectionFeature(this IServiceCollection services)
    {
        services.TryAddScoped<ReplayMusicTrackProjectionHandler>();
        services.TryAddScoped<ILoadStoredMusicTrackEventsPort, RavenLoadStoredMusicTrackEvents>();
        return services;
    }
}
