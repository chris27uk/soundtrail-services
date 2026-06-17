using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Soundtrail.Services.Enrichment.MusicTrackLookupCoordinator.Infrastructure.Messaging;

namespace Soundtrail.Services.Enrichment.MusicTrackLookupCoordinator.Infrastructure.CompositionRoot;

public static class AppServiceCollectionExtensions
{
    public static IServiceCollection AddMusicTrackLookupCoordinatorAppServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddMusicTrackLookupCoordinatorServiceBus(configuration);
        services.AddMusicTrackLookupCoordinatorFeature();
        return services;
    }
}
