using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Soundtrail.Services.Internal.Projector.Features.ProjectMusicTrackProjection.Adapters;

namespace Soundtrail.Services.Internal.Projector.Features.ProjectMusicTrackProjection.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddProjectMusicTrackProjectionFeature(this IServiceCollection services)
    {
        services.TryAddScoped<ProjectMusicTrackProjectionHandler>();
        services.TryAddSingleton<RavenMusicTrackProjectionMapper>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, MusicTrackProjectionSubscriptionHostedService>());
        return services;
    }
}
