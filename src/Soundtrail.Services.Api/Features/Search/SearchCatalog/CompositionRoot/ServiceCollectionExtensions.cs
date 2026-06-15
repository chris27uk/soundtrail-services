using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Domain;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Api.Features.Search.SearchCatalog.Adapters;
using Soundtrail.Services.Api.Features.Search.SearchCatalog.Ports;

namespace Soundtrail.Services.Api.Features.Search.SearchCatalog.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSearchCatalogFeature(
        this IServiceCollection services,
        Action<SearchCatalogFeatureOptions>? configure = null)
    {
        var options = new SearchCatalogFeatureOptions();
        configure?.Invoke(options);

        options.ConfigureQueueingDependencies?.Invoke(services);
        services.TryAddScoped<IQueueCatalogSearchAttempt>(sp => sp.GetRequiredService<IEnqueueCatalogSearchAttempt>());
        services.TryAddScoped<IQueueCatalogSearchAttemptPort>(sp => sp.GetRequiredService<IQueueCatalogSearchAttempt>());
        options.ConfigureCatalogSearchDependencies?.Invoke(services);
        services.TryAddScoped<IRecordCatalogSearchAttemptPort, RavenRecordCatalogSearchAttempt>();
        services.TryAddScoped<IHandler<SearchCatalogCommand, SearchCatalogResponse>, SearchCatalogHandler>();

        return services;
    }
}
