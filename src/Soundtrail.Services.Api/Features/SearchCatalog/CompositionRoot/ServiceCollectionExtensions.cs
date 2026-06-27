using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Api.Features.SearchCatalog.Adapters;
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
        services.TryAddScoped<ICatalogSearchDiscoveryRepository, RavenCatalogSearchDiscoveryRepository>();
        services.TryAddScoped<IApiHandler<SearchCatalogCommand, SearchCatalogResponse>, SearchCatalogHandler>();

        return services;
    }
}
