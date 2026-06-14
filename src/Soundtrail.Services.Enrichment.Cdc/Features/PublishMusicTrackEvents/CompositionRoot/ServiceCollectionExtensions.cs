using Microsoft.Extensions.DependencyInjection;
using Soundtrail.Services.Enrichment.Cdc.Features.PublishMusicTrackEvents;

namespace Soundtrail.Services.Enrichment.Cdc.Features.PublishMusicTrackEvents.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPublishMusicTrackEventsFeature(this IServiceCollection services)
    {
        services.AddHostedService<MusicTrackEventSubscriptionHostedService>();
        return services;
    }
}
