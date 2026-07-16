using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Raven.Client.Documents.Session;
using Soundtrail.Adapters.EventSourcing.CommonStores;
using Soundtrail.Adapters.Registry;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Discovery.Aggregates;

namespace Soundtrail.Adapters.EventSourcing.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCatalogSearchEventStreamRepository(this IServiceCollection services)
    {
        services.TryAddScoped<IEventStreamRepository<CatalogSearchId, IDomainEvent>>(
            sp => new RavenEventStreamRepository<CatalogSearchId, IDomainEvent>(
                sp.GetRequiredService<IAsyncDocumentSession>(),
                sp.GetRequiredService<ITypeRegistry>(),
                CatalogSearchEventStreamDefinition.Create()));

        return services;
    }
}
