using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Soundtrail.Services.Catalog.Projector.Features.ProjectMusicTrackCatalog.Adapters;

namespace Soundtrail.Services.Catalog.Projector.Features.ProjectMusicTrackCatalog.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddProjectMusicTrackCatalogFeature(this IServiceCollection services)
    {
        services.TryAddSingleton<ProjectMusicTrackCatalogHandler>();
        services.TryAddSingleton<CatalogMusicTrackProjectionApplier>();
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IHostedService, ProjectMusicTrackCatalogSubscriptionHostedService>());

        return services;
    }
}
