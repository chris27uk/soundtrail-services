using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Soundtrail.Services.Internal.Projector.Features.ProjectDiscoveryLifecycle.CompositionRoot;
using Soundtrail.Services.Internal.Projector.Features.ProjectMusicTrackProjection.CompositionRoot;
using Soundtrail.Services.Internal.Projector.Features.ProjectMusicTrackCatalog.CompositionRoot;
using Soundtrail.Services.Internal.Projector.Infrastructure.Raven;

namespace Soundtrail.Services.Internal.Projector.Infrastructure.CompositionRoot;

public static class AppServiceCollectionExtensions
{
    public static IServiceCollection AddInternalProjectorAppServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddRavenDocumentStore(configuration);
        services.AddProjectDiscoveryLifecycleFeature();
        services.AddProjectMusicTrackProjectionFeature();
        services.AddProjectMusicTrackCatalogFeature();
        return services;
    }
}
