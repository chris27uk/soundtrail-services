using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Domain;
using Soundtrail.Services.Api.Features.SearchMusic;
using Soundtrail.Services.Api.Features.SearchMusic.Queueing;
using Soundtrail.Services.Api.Features.SearchMusic.TrackSearch;
using Soundtrail.Services.Api.Features.SearchMusic.Tracks;

namespace Soundtrail.Services.Api.Features.SearchMusic.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSearchFeature(
        this IServiceCollection services,
        Action<SearchMusicFeatureOptions>? configure = null)
    {
        var options = new SearchMusicFeatureOptions();
        configure?.Invoke(options);

        services.AddSearchQueueingFeature(x => x.ConfigureDependencies = options.ConfigureQueueingDependencies);
        services.AddTrackSearchFeature(x => x.ConfigureDependencies = options.ConfigureTrackSearchDependencies);
        services.AddTrackModelsFeature();
        services.TryAddScoped<IHandler<SearchMusicRequest, SearchMusicResponse>, SearchMusicHandler>();

        return services;
    }
}
