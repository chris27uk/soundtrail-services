using Soundtrail.Services.Api.Features.Health;

namespace Soundtrail.Services.Api.Features.Health.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHealthFeature(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<TrackSearchReadyHealthCheck>("track-search-ready");

        return services;
    }
}
