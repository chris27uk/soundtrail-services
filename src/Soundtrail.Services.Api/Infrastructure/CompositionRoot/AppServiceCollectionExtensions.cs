using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Domain.CatalogBrowsing;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Api.Features.Albums.GetAlbum.CompositionRoot;
using Soundtrail.Services.Api.Features.Albums.ListTracksByAlbum.CompositionRoot;
using Soundtrail.Services.Api.Features.Artists.GetArtist.CompositionRoot;
using Soundtrail.Services.Api.Features.Artists.ListTracksByArtist.CompositionRoot;
using Soundtrail.Services.Api.Features.Search.Adapters;
using Soundtrail.Services.Api.Features.Search.CompositionRoot;
using Soundtrail.Services.Api.Features.Search.Ports;
using Soundtrail.Services.Api.Features.Tracks.GetTrack.CompositionRoot;
using Soundtrail.Services.Api.Infrastructure.Messaging;
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
        var options = new ApiAppServicesOptions
        {
            UseInMemoryQueueing = environment.IsEnvironment("Testing")
        };
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
            x.ConfigureQueueingDependencies = options.ConfigureQueueingDependencies ?? (svc =>
            {
                if (options.UseInMemoryQueueing)
                {
                    svc.TryAddSingleton<IEnqueueMusicRequest, InMemoryEnqueueMusicRequest>();
                    svc.TryAddSingleton<IQueueLookupMusicRequest>(sp => sp.GetRequiredService<IEnqueueMusicRequest>());
                    svc.TryAddSingleton<Soundtrail.Domain.Commands.IQueueLookupMusicRequestPort>(sp => sp.GetRequiredService<IQueueLookupMusicRequest>());
                }
                else
                {
                    svc.AddLookupMusicRequestQueue(configuration);
                }
            });

            x.ConfigureCatalogSearchDependencies = options.ConfigureCatalogSearchDependencies ?? (svc =>
            {
                if (environment.IsEnvironment("Testing"))
                {
                    svc.TryAddSingleton<ICatalogSearchPort, TestingNoOpCatalogSearchPort>();
                    return;
                }

                svc.AddRavenDocumentStore(configuration);
                svc.TryAddSingleton<ICatalogSearchPort, RavenCatalogSearch>();
            });
        });

        (options.ConfigureCatalogReadDependencies ?? (svc =>
        {
            if (environment.IsEnvironment("Testing"))
            {
                svc.TryAddSingleton<ICatalogReadPort, TestingNoOpCatalogReadPort>();
                return;
            }

            svc.AddRavenDocumentStore(configuration);
            svc.TryAddSingleton<ICatalogReadPort, RavenCatalogReadPort>();
        })).Invoke(services);

        return services;
    }
}
