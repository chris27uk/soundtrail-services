using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownTrackRequested.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOnKnownTrackRequestedFeature(this IServiceCollection services)
    {
        services.TryAddScoped<KnownTrackRequestedHandler>();
        return services;
    }
}
