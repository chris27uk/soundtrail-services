using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Raven.Client.Documents;
using Raven.Client.Documents.Conventions;
using Raven.Client.Documents.Session;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchPlannedForLookup.Adapters;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchPlannedForLookup.Ports;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchStatusChanged.Adapters;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchStatusChanged.Ports;
using Soundtrail.Services.Internal.Projector.Features.OnMusicCatalogChanged.Adapters;
using Soundtrail.Services.Internal.Projector.Features.OnMusicCatalogChanged.ProjectionModel;
using Soundtrail.Services.Internal.Projector.Features.OnMusicTrackChanged.Adapters;
using Soundtrail.Services.Internal.Projector.Features.OnMusicTrackChanged.Ports;
using Soundtrail.Services.Internal.Projector.Features.OnMusicTrackSearchStarted.Adapters;
using Soundtrail.Services.Internal.Projector.Features.OnMusicTrackSearchStarted.Ports;
using Soundtrail.Translators.ProjectionDocuments;
using Soundtrail.Translators.Registry.CompositionRoot;

namespace Soundtrail.Services.Internal.Projector.Infrastructure.Raven;

public static class RavenServiceCollectionExtensions
{
    public static IServiceCollection AddRavenDocumentStore(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddTypeTranslationsFromAssemblies(typeof(Soundtrail.Translators.Registry.TypeTranslationRegistry).Assembly);
        services.Configure<RavenDbOptions>(configuration.GetSection(RavenDbOptions.SectionName));

        services.TryAddSingleton<IDocumentStore>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<RavenDbOptions>>().Value;
            var store = new DocumentStore
            {
                Urls = options.Urls,
                Database = options.Database,
                Conventions = new DocumentConventions
                {
                    FindCollectionName = type => type.Name
                }
            };

            return store.Initialize();
        });
        services.TryAddScoped<IAsyncDocumentSession>(sp => sp.GetRequiredService<IDocumentStore>().OpenAsyncSession());
        services.TryAddScoped<ILoadDiscoveryLifecycleProjectionPort, RavenLoadDiscoveryLifecycleProjection>();
        services.TryAddScoped<ISaveDiscoveryLifecycleProjectionPort, RavenSaveDiscoveryLifecycleProjection>();
        services.TryAddScoped<ILoadCatalogSearchPlannedTrackingPort, RavenLoadCatalogSearchPlannedTracking>();
        services.TryAddScoped<ILoadCatalogSearchPlannedMusicTrackPort, RavenLoadCatalogSearchPlannedMusicTrack>();
        services.TryAddSingleton<RavenDiscoveryLifecycleProjectionMapper>();
        services.TryAddScoped<ILoadCatalogSearchStartedMusicTrackPort, RavenLoadCatalogSearchStartedMusicTrack>();
        services.TryAddScoped<ILoadCatalogSearchStartedTrackingPort, RavenLoadCatalogSearchStartedTracking>();
        services.TryAddScoped<ISaveCatalogSearchStartedTrackingPort, RavenSaveCatalogSearchStartedTracking>();
        services.TryAddScoped<ILoadPotentialCatalogLookupWorkPort, RavenLoadPotentialCatalogLookupWork>();
        services.TryAddScoped<ISavePotentialCatalogLookupWorkPort, RavenSavePotentialCatalogLookupWork>();
        services.TryAddScoped<ILoadMusicTrackProjectionPort, RavenLoadMusicTrackProjection>();
        services.TryAddScoped<ISaveMusicTrackProjectionPort, RavenSaveMusicTrackProjection>();
        services.TryAddSingleton<RavenMusicTrackProjectionMapper>();
        services.TryAddScoped<ILoadMusicTrackCatalogProjectionPort, RavenLoadMusicTrackCatalogProjection>();
        services.TryAddScoped<ISaveMusicTrackCatalogProjectionPort, RavenSaveMusicTrackCatalogProjection>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, RavenDatabaseHostedService>());

        return services;
    }
}
