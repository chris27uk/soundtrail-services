using Raven.Client.Documents;
using Raven.Client.Documents.Conventions;
using Raven.Client.Documents.Session;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Soundtrail.Services.Api.Infrastructure.Raven;

public static class RavenServiceCollectionExtensions
{
    public static IServiceCollection AddRavenDocumentStore(
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

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IHostedService, RavenDevelopmentSeedHostedService>());
        return services;
    }
}
