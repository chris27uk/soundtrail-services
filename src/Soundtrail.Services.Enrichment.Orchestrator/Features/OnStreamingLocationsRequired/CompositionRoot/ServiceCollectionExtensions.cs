using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnStreamingLocationsRequired.Adapters;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnStreamingLocationsRequired.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOnStreamingLocationsRequiredFeature(this IServiceCollection services)
    {
        services.TryAddScoped<StreamingLocationsRequiredHandler>();
        services.TryAddScoped<StreamingLocationsRequiredListener>();
        return services;
    }
}
