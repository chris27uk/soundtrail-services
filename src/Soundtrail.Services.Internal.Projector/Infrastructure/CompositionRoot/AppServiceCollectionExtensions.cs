using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchStatusChanged.CompositionRoot;
using Soundtrail.Services.Internal.Projector.Features.OnMusicTrackChanged.CompositionRoot;
using Soundtrail.Services.Internal.Projector.Features.OnMusicCatalogChanged.CompositionRoot;
using Soundtrail.Services.Internal.Projector.Infrastructure.Raven;

namespace Soundtrail.Services.Internal.Projector.Infrastructure.CompositionRoot;

public static class AppServiceCollectionExtensions
{
    public static IServiceCollection AddInternalProjectorAppServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddRavenDocumentStore(configuration);
        services.AddOnCatalogSearchStatusChangedFeature();
        services.AddOnMusicTrackChangedFeature();
        services.AddOnMusicCatalogChangedFeature();
        return services;
    }
}
