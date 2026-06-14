using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Domain;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Api.Features.Search.Ports;

namespace Soundtrail.Services.Api.Features.Search.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSearchCatalogFeature(
        this IServiceCollection services,
        Action<SearchCatalogFeatureOptions>? configure = null)
    {
        var options = new SearchCatalogFeatureOptions();
        configure?.Invoke(options);

        options.ConfigureQueueingDependencies?.Invoke(services);
        services.TryAddScoped<IQueueLookupMusicRequest>(sp => sp.GetRequiredService<IEnqueueMusicRequest>());
        services.TryAddScoped<IQueueLookupMusicRequestPort>(sp => sp.GetRequiredService<IQueueLookupMusicRequest>());
        options.ConfigureCatalogSearchDependencies?.Invoke(services);
        services.TryAddScoped<IHandler<SearchCatalogCommand, SearchCatalogResponse>, SearchCatalogHandler>();

        return services;
    }
}
