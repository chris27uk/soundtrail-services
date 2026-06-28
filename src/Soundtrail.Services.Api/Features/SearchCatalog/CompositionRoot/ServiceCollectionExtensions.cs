using Raven.Client.Documents.Session;
using Soundtrail.Adapters.Discovery;
using Soundtrail.Adapters.EventSourcing;
using Soundtrail.Adapters.Registry;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Api.Features.SearchCatalog.Ports;

namespace Soundtrail.Services.Api.Features.SearchCatalog.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSearchCatalogFeature(
        this IServiceCollection services,
        Action<SearchCatalogFeatureOptions>? configure = null)
    {
        var options = new SearchCatalogFeatureOptions();
        configure?.Invoke(options);

        options.ConfigureQueueingDependencies?.Invoke(services);
        options.ConfigureCatalogSearchDependencies?.Invoke(services);
        services.TryAddScoped<IEventStreamRepository<DiscoveryQueryKey, IDomainEvent>>(sp =>
            new RavenEventStreamRepository<DiscoveryQueryKey, IDomainEvent>(
                sp.GetRequiredService<IAsyncDocumentSession>(),
                sp.GetRequiredService<ITypeRegistry>(),
                DiscoveryQueryEventStreamDefinition.Create()));
        services.TryAddScoped<IApiHandler<SearchCatalogCommand, SearchCatalogResponse>, SearchCatalogHandler>();

        return services;
    }
}
