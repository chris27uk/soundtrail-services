using Microsoft.Extensions.Logging;
using Soundtrail.Tools.MusicBrainzImport.Features.ReplayDiscoveryLifecycleProjection;
using Soundtrail.Tools.MusicBrainzImport.Features.ReplayCatalogProjection;
using Soundtrail.Tools.MusicBrainzImport.Features.RebuildAllReadModels;

namespace Soundtrail.Tools.MusicBrainzImport.Features.ImportMusicBrainzDump.Adapters;

public sealed class MusicBrainzDumpCommandLineRunner(
    ImportMusicBrainzDumpHandler handler,
    ReplayCatalogProjectionHandler replayCatalogProjectionHandler,
    ReplayDiscoveryLifecycleProjectionBatchHandler replayDiscoveryLifecycleProjectionBatchHandler,
    RebuildAllReadModelsHandler rebuildAllReadModelsHandler,
    ILogger<MusicBrainzDumpCommandLineRunner> logger)
{
    public async Task<int> RunAsync(
        MusicBrainzToolCommand command,
        CancellationToken cancellationToken)
    {
        switch (command)
        {
            case MusicBrainzToolCommand.Import import:
                var importResult = await handler.Handle(import.Command, cancellationToken);
                logger.LogInformation(
                    "Imported MusicBrainz dump records. Processed: {Processed}, Imported: {Imported}, Skipped: {Skipped}",
                    importResult.ProcessedRecordCount,
                    importResult.ImportedRecordCount,
                    importResult.SkippedRecordCount);

                Console.WriteLine(
                    $"Processed={importResult.ProcessedRecordCount} Imported={importResult.ImportedRecordCount} Skipped={importResult.SkippedRecordCount}");
                break;
            case MusicBrainzToolCommand.ReplayCatalog replayCatalog:
                var replayResult = await replayCatalogProjectionHandler.Handle(replayCatalog.Command, cancellationToken);
                logger.LogInformation(
                    "Replayed catalog projections. Streams: {Streams}, Events: {Events}",
                    replayResult.ReplayedStreamCount,
                    replayResult.ReplayedEventCount);

                Console.WriteLine(
                    $"ReplayedStreams={replayResult.ReplayedStreamCount} ReplayedEvents={replayResult.ReplayedEventCount}");
                break;
            case MusicBrainzToolCommand.ReplayDiscoveryLifecycle replayDiscoveryLifecycle:
                var replayDiscoveryResult = await replayDiscoveryLifecycleProjectionBatchHandler.Handle(
                    replayDiscoveryLifecycle.Command,
                    cancellationToken);
                logger.LogInformation(
                    "Replayed discovery lifecycle projections. Criteria: {Criteria}, Events: {Events}",
                    replayDiscoveryResult.ReplayedCriteriaCount,
                    replayDiscoveryResult.ReplayedEventCount);

                Console.WriteLine(
                    $"ReplayedCriteria={replayDiscoveryResult.ReplayedCriteriaCount} ReplayedEvents={replayDiscoveryResult.ReplayedEventCount}");
                break;
            case MusicBrainzToolCommand.RebuildAllReadModels rebuildAllReadModels:
                var rebuildResult = await rebuildAllReadModelsHandler.Handle(
                    rebuildAllReadModels.Command,
                    cancellationToken);
                logger.LogInformation(
                    "Rebuilt all read models. ClearedPotentialCatalogLookupWork={ClearedPotentialCatalogLookupWork}, ClearedCatalogSearchTracking={ClearedCatalogSearchTracking}, ClearedActiveLookupWork={ClearedActiveLookupWork}, PlannerStreams={PlannerStreams}, CatalogStreams={CatalogStreams}, DiscoveryCriteria={DiscoveryCriteria}",
                    rebuildResult.ClearedPotentialCatalogLookupWorkCount,
                    rebuildResult.ClearedCatalogSearchTrackingCount,
                    rebuildResult.ClearedActiveLookupWorkCount,
                    rebuildResult.PlannerMusicTrackReplayedStreamCount,
                    rebuildResult.CatalogReplayedStreamCount,
                    rebuildResult.DiscoveryLifecycleReplayedCriteriaCount);

                Console.WriteLine(
                    $"ClearedPotentialCatalogLookupWork={rebuildResult.ClearedPotentialCatalogLookupWorkCount} ClearedCatalogSearchTracking={rebuildResult.ClearedCatalogSearchTrackingCount} ClearedActiveLookupWork={rebuildResult.ClearedActiveLookupWorkCount} PlannerReplayedStreams={rebuildResult.PlannerMusicTrackReplayedStreamCount} PlannerReplayedEvents={rebuildResult.PlannerMusicTrackReplayedEventCount} CatalogReplayedStreams={rebuildResult.CatalogReplayedStreamCount} CatalogReplayedEvents={rebuildResult.CatalogReplayedEventCount} DiscoveryReplayedCriteria={rebuildResult.DiscoveryLifecycleReplayedCriteriaCount} DiscoveryReplayedEvents={rebuildResult.DiscoveryLifecycleReplayedEventCount}");
                break;
            default:
                throw new InvalidOperationException($"Unsupported tool command type '{command.GetType().Name}'.");
        }

        return 0;
    }
}
