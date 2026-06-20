using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Soundtrail.Services.Catalog.Projector.Features.ProjectMusicTrackCatalog.CompositionRoot;
using Soundtrail.Services.Catalog.Projector.Features.ReplayMusicTrackCatalogProjection.CompositionRoot;
using Soundtrail.Services.Catalog.Projector.Infrastructure.Raven;

namespace Soundtrail.Services.Catalog.Projector.Infrastructure.CompositionRoot;

public static class AppServiceCollectionExtensions
{
    public static IServiceCollection AddCatalogProjectorAppServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddRavenDocumentStore(configuration);
        services.AddProjectMusicTrackCatalogFeature();
        services.AddReplayMusicTrackCatalogProjectionFeature();
        return services;
    }
}
