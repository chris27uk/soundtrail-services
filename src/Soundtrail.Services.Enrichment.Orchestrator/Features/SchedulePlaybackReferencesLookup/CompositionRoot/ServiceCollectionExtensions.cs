using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.SchedulePlaybackReferencesLookup.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSchedulePlaybackReferencesLookupFeature(this IServiceCollection services)
    {
        services.TryAddScoped<SchedulePlaybackReferencesLookupHandler>();
        services.TryAddScoped<Adapters.MusicTrackEventListener>();
        return services;
    }
}
