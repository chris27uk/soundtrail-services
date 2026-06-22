using Soundtrail.Domain;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Responses;
using Soundtrail.Tools.MusicBrainzImport.Features.ReplayCatalogProjection;
using Soundtrail.Tools.MusicBrainzImport.Features.ReplayDiscoveryLifecycleProjection;
using Soundtrail.Tools.MusicBrainzImport.Features.ReplayPlannerMusicTrackProjection;

namespace Soundtrail.Tools.MusicBrainzImport.Features.RebuildAllReadModels;

public sealed class RebuildAllReadModelsHandler(
    ReplayPlannerMusicTrackProjectionBatchHandler replayPlannerMusicTrackProjectionBatchHandler,
    ReplayCatalogProjectionHandler replayCatalogProjectionHandler,
    ReplayDiscoveryLifecycleProjectionBatchHandler replayDiscoveryLifecycleProjectionBatchHandler,
    IClearPlannerOperationalStatePort clearPlannerOperationalStatePort) : IHandler<RebuildAllReadModelsCommand, RebuildAllReadModelsResult>
{
    public async Task<RebuildAllReadModelsResult> Handle(
        RebuildAllReadModelsCommand command,
        CancellationToken cancellationToken = default)
    {
        var cleared = await clearPlannerOperationalStatePort.ClearAsync(cancellationToken);

        var plannerTrack = await replayPlannerMusicTrackProjectionBatchHandler.Handle(
            new ReplayMusicTrackProjectionBatchCommand(),
            cancellationToken);
        var catalog = await replayCatalogProjectionHandler.Handle(
            new ReplayCatalogProjectionCommand(),
            cancellationToken);
        var discovery = await replayDiscoveryLifecycleProjectionBatchHandler.Handle(
            new ReplayDiscoveryLifecycleProjectionBatchCommand(),
            cancellationToken);

        return new RebuildAllReadModelsResult(
            plannerTrack.ReplayedStreamCount,
            plannerTrack.ReplayedEventCount,
            catalog.ReplayedStreamCount,
            catalog.ReplayedEventCount,
            discovery.ReplayedCriteriaCount,
            discovery.ReplayedEventCount,
            cleared.ClearedPotentialCatalogLookupWorkCount,
            cleared.ClearedCatalogSearchTrackingCount,
            cleared.ClearedActiveLookupWorkCount);
    }
}
