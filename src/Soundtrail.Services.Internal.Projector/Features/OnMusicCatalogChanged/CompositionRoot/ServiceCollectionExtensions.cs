using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Soundtrail.Services.Internal.Projector.Features.OnMusicCatalogChanged.Adapters;

namespace Soundtrail.Services.Internal.Projector.Features.OnMusicCatalogChanged.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddProjectMusicTrackCatalogFeature(this IServiceCollection services)
    {
        services.TryAddScoped<ProjectMusicTrackCatalogHandler>();
        services.TryAddSingleton<RavenMusicTrackCatalogProjectionMapper>();
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IHostedService, ProjectMusicTrackCatalogSubscriptionHostedService>());

        return services;
    }
}
