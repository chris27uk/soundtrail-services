using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Soundtrail.Services.Public.Projector.Features.PublishMusicTrackEvents.CompositionRoot;
using Soundtrail.Services.Public.Projector.Infrastructure.Messaging;
using Soundtrail.Services.Public.Projector.Infrastructure.Raven;

namespace Soundtrail.Services.Public.Projector.Infrastructure.CompositionRoot;

public static class AppServiceCollectionExtensions
{
    public static IServiceCollection AddPublicProjectorAppServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddPublicProjectorServiceBus(configuration);
        services.AddPublicProjectorRavenDocumentStore(configuration);
        services.AddPublishMusicTrackEventsFeature();
        return services;
    }
}
