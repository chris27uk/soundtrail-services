using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Services.Internal.Projector.Features.OnReplayMusicTrack.Adapters;
using Soundtrail.Services.Internal.Projector.Features.OnReplayMusicTrack.StoredEvents;

namespace Soundtrail.Services.Internal.Projector.Features.OnReplayMusicTrack.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOnReplayMusicTrackFeature(this IServiceCollection services)
    {
        services.TryAddScoped<ReplayMusicTrackHandler>();
        services.TryAddScoped<ILoadStoredMusicTrackEventsPort, RavenLoadStoredMusicTrackEvents>();
        return services;
    }
}
