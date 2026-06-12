using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Domain;
using Soundtrail.Services.Api.Features.Search.Queueing;
using Soundtrail.Services.Api.Features.Search.TrackSearch;
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
        services.AddTrackSearchFeature(x => x.ConfigureDependencies = options.ConfigureTrackSearchDependencies);
        services.AddTrackModelsFeature();
        services.TryAddScoped<IHandler<SearchMusicRequest, SearchMusicResponse>, SearchMusicHandler>();

        return services;
    }
}
