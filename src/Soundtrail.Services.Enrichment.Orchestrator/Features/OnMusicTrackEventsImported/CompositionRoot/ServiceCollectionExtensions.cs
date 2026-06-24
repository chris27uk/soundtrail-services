using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnMusicTrackEventsImported.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOnMusicTrackEventsImportedFeature(this IServiceCollection services)
    {
        services.TryAddScoped<MusicTrackEventsImportedHandler>();
        return services;
    }
}
