using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Services.Api.Features.GetAlbum.CompositionRoot;
using Soundtrail.Services.Api.Features.GetArtist.CompositionRoot;
using Soundtrail.Services.Api.Features.GetTrack.CompositionRoot;
using Soundtrail.Services.Api.Features.ListTracksByAlbum.CompositionRoot;
using Soundtrail.Services.Api.Features.ListTracksByArtist.CompositionRoot;
using Soundtrail.Services.Api.Features.SearchCatalog.CompositionRoot;
using Soundtrail.Services.Api.Features.SearchCatalog.Ports;
using Soundtrail.Services.Api.Infrastructure.Messaging;
using Soundtrail.Services.Api.Infrastructure.Ports;
using Soundtrail.Services.Api.Infrastructure.Raven;
using Soundtrail.Services.Api.Infrastructure.Time;

namespace Soundtrail.Services.Api.Infrastructure.CompositionRoot;

public static class AppServiceCollectionExtensions
{
    public static IServiceCollection AddApiAppServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment,
        Action<ApiAppServicesOptions>? configure = null)
    {
        var options = new ApiAppServicesOptions();
        configure?.Invoke(options);

        options.ConfigureClockDependencies?.Invoke(services);
        services.TryAddSingleton<IClockPort, SystemClock>();

        services.AddGetArtistFeature();
        services.AddListTracksByArtistFeature();
        services.AddGetAlbumFeature();
        services.AddListTracksByAlbumFeature();
        services.AddGetTrackFeature();
        services.AddSearchCatalogFeature(x =>
        {
            x.ConfigureQueueingDependencies = options.ConfigureQueueingDependencies ?? (svc => svc.AddCatalogSearchAttemptQueue(configuration));

            x.ConfigureCatalogSearchDependencies = options.ConfigureCatalogSearchDependencies ?? (svc =>
            {
                svc.AddRavenDocumentStore(configuration);
                svc.TryAddSingleton<ICatalogSearchPort, RavenCatalogSearch>();
            });
        });

        (options.ConfigureCatalogReadDependencies ?? (svc =>
        {
            svc.AddRavenDocumentStore(configuration);
            svc.TryAddSingleton<ICatalogReadPort, RavenCatalogReadPort>();
        })).Invoke(services);

        return services;
    }
}
