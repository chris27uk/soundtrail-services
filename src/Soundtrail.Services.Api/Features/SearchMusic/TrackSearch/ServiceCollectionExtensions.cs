namespace Soundtrail.Services.Api.Features.SearchMusic.TrackSearch;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTrackSearchFeature(
        this IServiceCollection services,
        Action<TrackSearchFeatureOptions>? configure = null)
    {
        var options = new TrackSearchFeatureOptions();
        configure?.Invoke(options);
        options.ConfigureDependencies?.Invoke(services);
        return services;
    }
}
