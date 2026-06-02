using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Raven.Client.Documents;
using Raven.Client.Documents.Conventions;
using Raven.Client.Documents.Indexes;
using Soundtrail.Services.Enrichment.Features.Scheduling.Contracts;

namespace Soundtrail.Services.Enrichment.Worker.Infrastructure.Raven;

public static class RavenServiceCollectionExtensions
{
    public static IServiceCollection AddWorkerRavenDocumentStore(
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

        services.AddSingleton<IRankedMusicCandidateStore, RavenRankedMusicCandidateStore>();
        services.AddSingleton<IMusicCatalogCandidateSearch, RavenMusicCatalogCandidateSearch>();
        services.AddSingleton<IActiveLookupWorkStore, RavenActiveLookupWorkStore>();
        return services;
    }
}
