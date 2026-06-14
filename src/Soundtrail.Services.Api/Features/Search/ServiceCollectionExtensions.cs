using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Api.Features.Search.Queueing;
using Soundtrail.Services.Api.Features.Search.Tracks;

namespace Soundtrail.Services.Api.Features.Search;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSearchFeature(
        this IServiceCollection services,
        Action<SearchFeatureOptions>? configure = null)
    {
        var options = new SearchFeatureOptions();
        configure?.Invoke(options);

        services.AddSearchQueueingFeature(x => x.ConfigureDependencies = options.ConfigureQueueingDependencies);
        services.TryAddScoped<IQueueLookupMusicRequestPort>(sp => sp.GetRequiredService<IEnqueueMusicRequest>());
        services.AddTrackModelsFeature();
        options.ConfigureCatalogSearchDependencies?.Invoke(services);
        services.TryAddScoped<IHandler<SearchCatalogCommand, SearchCatalogResponse>, SearchCatalogHandler>();

        return services;
    }
}
