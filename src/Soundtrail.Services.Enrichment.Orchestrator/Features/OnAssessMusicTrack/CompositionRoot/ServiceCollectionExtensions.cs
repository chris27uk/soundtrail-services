using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnAssessMusicTrack.Adapters;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnAssessMusicTrack.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOnAssessMusicTrackFeature(this IServiceCollection services)
    {
        services.TryAddScoped<AssessMusicTrackHandler>();
        services.TryAddScoped<AssessMusicTrackListener>();
        return services;
    }
}
