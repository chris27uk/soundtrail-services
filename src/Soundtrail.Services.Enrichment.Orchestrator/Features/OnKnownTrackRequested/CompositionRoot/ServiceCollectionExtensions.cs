using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownTrackRequested.Adapters;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownTrackRequested.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOnKnownTrackRequestedFeature(this IServiceCollection services)
    {
        services.TryAddScoped<KnownTrackRequestedHandler>();
        services.TryAddScoped<KnownTrackRequestedListener>();
        return services;
    }
}
