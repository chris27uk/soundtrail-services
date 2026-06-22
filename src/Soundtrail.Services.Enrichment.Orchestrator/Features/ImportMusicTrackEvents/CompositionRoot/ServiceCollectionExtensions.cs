using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.ImportMusicTrackEvents.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddImportMusicTrackEventsFeature(this IServiceCollection services)
    {
        services.TryAddScoped<ImportMusicTrackEventsHandler>();
        return services;
    }
}
