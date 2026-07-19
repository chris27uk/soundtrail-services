using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Raven.Client.Documents.Session;
using Soundtrail.Adapters.TypeRegistry;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Discovery.Aggregates;

namespace Soundtrail.Adapters.EventSourcing.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCatalogSearchEventStreamRepository(this IServiceCollection services)
    {
        services.TryAddScoped<IEventStreamRepository<CatalogWorkId>>(
            sp => new RavenEventStreamRepository<CatalogWorkId>(
                sp.GetRequiredService<IAsyncDocumentSession>(),
                sp.GetRequiredService<ITypeRegistry>(),
                "catalog-stream"));

        return services;
    }

    public static IServiceCollection AddArtistCatalogEventStreamRepository(this IServiceCollection services)
    {
        services.TryAddScoped<IEventStreamRepository<ArtistId>>(
            sp => new RavenEventStreamRepository<ArtistId>(
                sp.GetRequiredService<IAsyncDocumentSession>(),
                sp.GetRequiredService<ITypeRegistry>(),
                "artist-catalog-stream"));

        return services;
    }
}
