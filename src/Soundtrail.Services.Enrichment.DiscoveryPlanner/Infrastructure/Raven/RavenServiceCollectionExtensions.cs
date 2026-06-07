using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Raven.Client.Documents;
using Raven.Client.Documents.Conventions;
using Raven.Client.Documents.Indexes;
using Raven.Client.Documents.Session;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Idempotency;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.MusicTracks;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Persistence;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Search;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Infrastructure.Raven;

public static class RavenServiceCollectionExtensions
{
    public static IServiceCollection AddSchedulerRavenDocumentStore(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<RavenDbOptions>(configuration.GetSection(RavenDbOptions.SectionName));
        services.AddSingleton<IDocumentStore>(sp =>
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

            store.Initialize();
            IndexCreation.CreateIndexes(typeof(Indexes.TrackCatalogue_BySearchText).Assembly, store);
            return store;
        });

        services.AddScoped<IRankedMusicCandidateStore, RavenRankedMusicCandidateStore>();
        services.AddScoped<IActiveLookupWorkStore, RavenActiveLookupWorkStore>();
        services.AddScoped<IMusicTrackEventRepository, RavenMusicTrackStreamStore>();
        services.AddScoped<IMusicTrackProjectionStore, RavenMusicTrackProjectionStore>();
        services.AddScoped<IProviderSnapshotStore, RavenProviderSnapshotStore>();
        services.AddSingleton<IMusicCatalogCandidateSearch, RavenMusicCatalogCandidateSearch>();
        return services;
    }
}
