namespace Soundtrail.Services.Api.Features.Health;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHealthFeature(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<TrackSearchReadyHealthCheck>("track-search-ready");

        return services;
    }
}
