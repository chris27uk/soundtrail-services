using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Raven.Client.Documents;
using Raven.Client.Documents.Conventions;
using Raven.Client.Documents.Session;
using Soundtrail.Services.Internal.Projector.Features.OnMusicTrackChanged.Ports;
using Soundtrail.Domain.Model;
using Soundtrail.Services.Internal.Projector.Features.OnMusicCatalogChanged.Adapters;
using Soundtrail.Services.Internal.Projector.Features.OnMusicCatalogChanged.ProjectionModel;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnMusicCatalogLookupAttempted.Adapters;
using Soundtrail.Services.Internal.Projector.Features.OnMusicTrackChanged.Adapters;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchStatusChanged.Ports;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchStatusChanged.Adapters;
using Soundtrail.Services.Enrichment.Orchestrator.Infrastructure.Raven;

namespace Soundtrail.Tools.MusicBrainzImport.Infrastructure.Raven;

public static class RavenServiceCollectionExtensions
{
    public static IServiceCollection AddMusicBrainzImportRaven(
        this IServiceCollection services,
        IConfiguration configuration)
    {
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
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, RavenDatabaseHostedService>());

        services.TryAddScoped<IMusicTrackEventRepository, RavenMusicTrackStreamStore>();
        services.TryAddScoped<ILoadMusicTrackCatalogProjectionPort, RavenLoadMusicTrackCatalogProjection>();
        services.TryAddScoped<ISaveMusicTrackCatalogProjectionPort, RavenSaveMusicTrackCatalogProjection>();
        services.TryAddSingleton<RavenMusicTrackCatalogProjectionMapper>();
        services.TryAddScoped<ILoadMusicTrackProjectionPort, RavenLoadMusicTrackProjection>();
        services.TryAddScoped<ISaveMusicTrackProjectionPort, RavenSaveMusicTrackProjection>();
        services.TryAddSingleton<RavenMusicTrackProjectionMapper>();
        services.TryAddScoped<ILoadDiscoveryLifecycleProjectionPort, RavenLoadDiscoveryLifecycleProjection>();
        services.TryAddScoped<ISaveDiscoveryLifecycleProjectionPort, RavenSaveDiscoveryLifecycleProjection>();
        services.TryAddSingleton<RavenDiscoveryLifecycleProjectionMapper>();

        return services;
    }
}
