using Raven.Client.Documents;
using Raven.Client.Documents.Conventions;
using Microsoft.Extensions.Options;

namespace Soundtrail.Services.Api.Infrastructure.Raven;

public static class RavenServiceCollectionExtensions
{
    public static IServiceCollection AddRavenDocumentStore(
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

            return store.Initialize();
        });

        services.AddHostedService<RavenDevelopmentSeedHostedService>();
        return services;
    }
}
