using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Raven.Client.Documents;
using Raven.Client.Documents.Conventions;
using Raven.Client.Documents.Session;
using Soundtrail.Services.Internal.Projector.Features.ProjectMusicTrackCatalog.Adapters;
using Soundtrail.Services.Internal.Projector.Features.ProjectMusicTrackCatalog.ProjectionModel;

namespace Soundtrail.Services.Internal.Projector.Infrastructure.Raven;

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
        services.TryAddScoped<ILoadMusicTrackCatalogProjectionPort, RavenLoadMusicTrackCatalogProjection>();
        services.TryAddScoped<ISaveMusicTrackCatalogProjectionPort, RavenSaveMusicTrackCatalogProjection>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, RavenDatabaseHostedService>());

        return services;
    }
}
