using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Raven.Client.Documents;
using Raven.Client.Documents.Conventions;
using Raven.Client.Documents.Session;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Enrichment.Orchestrator.Features.BacklogScheduling.Adapters;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Idempotency;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Persistence;
using Soundtrail.Contracts;
using Soundtrail.Services.Enrichment.Orchestrator.Features.EnrichmentResponse.Adapters;
using Soundtrail.Services.Enrichment.Orchestrator.Features.JustInTimeScheduling.Adapters;
using Soundtrail.Services.Enrichment.Orchestrator.Features.ProjectDiscoveryLifecycle.Adapters;
using Soundtrail.Services.Enrichment.Orchestrator.Features.ProjectMusicTrackProjection.Adapters;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Search;
using Soundtrail.Domain.Model;

namespace Soundtrail.Services.Enrichment.Orchestrator.Infrastructure.Raven;

public static class RavenServiceCollectionExtensions
{
    public static IServiceCollection AddSchedulerRavenDocumentStore(
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
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, RavenIndexesHostedService>());

        services.TryAddScoped<IPotentialCatalogLookupWorkStore, RavenPotentialCatalogLookupWorkStore>();
        services.TryAddScoped<ICatalogSearchTrackingStore, RavenCatalogSearchTrackingStore>();
        services.TryAddScoped<IActiveLookupWorkStore, RavenActiveLookupWorkStore>();
        services.TryAddScoped<IMusicTrackEventRepository, RavenMusicTrackStreamStore>();
        services.TryAddScoped<ILoadDiscoveryLifecycleProjectionPort, RavenLoadDiscoveryLifecycleProjection>();
        services.TryAddScoped<ISaveDiscoveryLifecycleProjectionPort, RavenSaveDiscoveryLifecycleProjection>();
        services.TryAddScoped<ILoadMusicTrackProjectionPort, RavenLoadMusicTrackProjection>();
        services.TryAddScoped<ISaveMusicTrackProjectionPort, RavenSaveMusicTrackProjection>();
        services.TryAddSingleton<IMusicCatalogCandidateSearch, RavenMusicCatalogCandidateSearch>();
        services.TryAddSingleton<ILocalMusicTrackSearch, RavenLocalMusicTrackSearch>();
        return services;
    }
}
