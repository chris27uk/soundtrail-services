using Microsoft.Extensions.DependencyInjection;

namespace Soundtrail.Services.Enrichment.MusicTrackLookupCoordinator.Infrastructure.CompositionRoot;

public static class AppServiceCollectionExtensions
{
    public static IServiceCollection AddMusicTrackLookupCoordinatorAppServices(this IServiceCollection services)
    {
        services.AddMusicTrackLookupCoordinatorFeature();
        return services;
    }
}
