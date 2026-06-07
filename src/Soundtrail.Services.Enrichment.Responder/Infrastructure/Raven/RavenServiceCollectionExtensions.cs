using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Raven.Client.Documents;
using Raven.Client.Documents.Conventions;
using Raven.Client.Documents.Indexes;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.MusicTracks;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Infrastructure.Raven;

namespace Soundtrail.Services.Enrichment.Responder.Infrastructure.Raven;

public static class RavenServiceCollectionExtensions
{
    public static IServiceCollection AddResponderRavenDocumentStore(
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
            IndexCreation.CreateIndexes(
                typeof(Soundtrail.Services.Enrichment.DiscoveryPlanner.Infrastructure.Raven.RavenServiceCollectionExtensions).Assembly,
                store);
            return store;
        });

        services.AddScoped<IMusicTrackEventRepository, RavenMusicTrackStreamStore>();
        services.AddScoped<IMusicTrackProjectionStore, RavenMusicTrackProjectionStore>();
        services.AddScoped<IProviderSnapshotStore, RavenProviderSnapshotStore>();
        return services;
    }
}
