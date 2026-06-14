namespace Soundtrail.Services.Api.Features.Health.TrackSearchReady.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTrackSearchReadyFeature(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<TrackSearchReadyHealthCheck>("track-search-ready");

        return services;
    }
}
