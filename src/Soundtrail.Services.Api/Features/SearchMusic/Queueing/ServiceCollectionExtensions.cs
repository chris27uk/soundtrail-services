namespace Soundtrail.Services.Api.Features.SearchMusic.Queueing;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSearchQueueingFeature(
        this IServiceCollection services,
        Action<SearchQueueingFeatureOptions>? configure = null)
    {
        var options = new SearchQueueingFeatureOptions();
        configure?.Invoke(options);
        options.ConfigureDependencies?.Invoke(services);
        return services;
    }
}
