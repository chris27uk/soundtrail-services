using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Raven.Client.Documents;
using Raven.Client.Documents.Conventions;

namespace Soundtrail.Services.Enrichment.Cdc.Infrastructure.Raven;

public static class RavenServiceCollectionExtensions
{
    public static IServiceCollection AddCdcRavenDocumentStore(
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
            return store;
        });

        return services;
    }
}
