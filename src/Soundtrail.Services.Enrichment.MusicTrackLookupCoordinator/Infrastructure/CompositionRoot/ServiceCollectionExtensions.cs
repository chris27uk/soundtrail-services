using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Services.Enrichment.MusicTrackLookupCoordinator.Infrastructure.Messaging;

namespace Soundtrail.Services.Enrichment.MusicTrackLookupCoordinator.Infrastructure.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMusicTrackLookupCoordinatorFeature(this IServiceCollection services)
    {
        services.TryAddScoped<MusicTrackEventListener>();
        return services;
    }
}
