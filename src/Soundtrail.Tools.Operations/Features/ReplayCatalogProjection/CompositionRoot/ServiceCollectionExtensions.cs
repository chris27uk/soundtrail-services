using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Tools.MusicBrainzImport.Features.ReplayCatalogProjection.Adapters;
using Soundtrail.Tools.MusicBrainzImport.Features.ReplayCatalogProjection.EventStore;
using Soundtrail.Tools.MusicBrainzImport.Features.ReplayCatalogProjection.ProjectionReset;

namespace Soundtrail.Tools.MusicBrainzImport.Features.ReplayCatalogProjection.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddReplayCatalogProjectionFeature(this IServiceCollection services)
    {
        services.TryAddScoped<ReplayCatalogProjectionHandler>();
        services.TryAddScoped<ILoadCatalogProjectionReplayTargetsPort, RavenLoadCatalogProjectionReplayTargets>();
        services.TryAddScoped<ILoadMusicTrackEventsForCatalogReplayPort, RavenLoadMusicTrackEventsForCatalogReplay>();
        services.TryAddScoped<IResetCatalogProjectionCheckpointPort, RavenResetCatalogProjectionCheckpoint>();
        return services;
    }
}
