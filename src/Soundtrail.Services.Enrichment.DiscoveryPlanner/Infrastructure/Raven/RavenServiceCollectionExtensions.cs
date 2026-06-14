using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Raven.Client.Documents;
using Raven.Client.Documents.Conventions;
using Raven.Client.Documents.Session;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.BacklogScheduling.Adapters;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Idempotency;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Persistence;
using Soundtrail.Contracts;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.EnrichmentResponse.Adapters;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.Adapters;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.LocalSearch;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Search;
using Soundtrail.Domain.Model;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Infrastructure.Raven;

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

        services.TryAddScoped<IRankedMusicCandidateStore, RavenRankedMusicCandidateStore>();
        services.TryAddScoped<IActiveLookupWorkStore, RavenActiveLookupWorkStore>();
        services.TryAddScoped<IMusicTrackEventRepository, RavenMusicTrackStreamStore>();
        services.TryAddScoped<IMusicTrackProjectionStore, RavenMusicTrackProjectionStore>();
        services.TryAddScoped<IProviderSnapshotStore, RavenProviderSnapshotStore>();
        services.TryAddSingleton<MusicTrackProjectionApplier>();
        services.TryAddSingleton<IMusicCatalogCandidateSearch, RavenMusicCatalogCandidateSearch>();
        services.TryAddSingleton<ILocalMusicTrackSearch, RavenLocalMusicTrackSearch>();
        return services;
    }
}
