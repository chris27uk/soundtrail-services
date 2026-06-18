using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Soundtrail.Services.Enrichment.Cdc.Features.PublishMusicTrackEvents.Adapters;
using Soundtrail.Services.Enrichment.Cdc.Features.PublishMusicTrackEvents.Publishing;

namespace Soundtrail.Services.Enrichment.Cdc.Features.PublishMusicTrackEvents.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPublishMusicTrackEventsFeature(this IServiceCollection services)
    {
        services.TryAddScoped<PublishMusicTrackEventsHandler>();
        services.TryAddScoped<IPublishMusicTrackIntegrationEvents, WolverineMusicTrackIntegrationEventPublisher>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, MusicTrackEventSubscriptionHostedService>());
        return services;
    }
}
