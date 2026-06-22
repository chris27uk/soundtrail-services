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
                await handler.Handle(import.Command, cancellationToken);
                logger.LogInformation("Imported MusicBrainz dump records.");
                Console.WriteLine("Import completed.");
                break;
            case MusicBrainzToolCommand.ReplayCatalog replayCatalog:
                await replayCatalogProjectionHandler.Handle(replayCatalog.Command, cancellationToken);
                logger.LogInformation("Replayed catalog projections.");
                Console.WriteLine("Catalog replay completed.");
                break;
            case MusicBrainzToolCommand.ReplayDiscoveryLifecycle replayDiscoveryLifecycle:
                await replayDiscoveryLifecycleProjectionBatchHandler.Handle(
                    replayDiscoveryLifecycle.Command,
                    cancellationToken);
                logger.LogInformation("Replayed discovery lifecycle projections.");
                Console.WriteLine("Discovery lifecycle replay completed.");
                break;
            case MusicBrainzToolCommand.RebuildAllReadModels rebuildAllReadModels:
                await rebuildAllReadModelsHandler.Handle(
                    rebuildAllReadModels.Command,
                    cancellationToken);
                logger.LogInformation("Rebuilt all read models.");
                Console.WriteLine("Rebuild completed.");
                break;
            default:
                throw new InvalidOperationException($"Unsupported tool command type '{command.GetType().Name}'.");
        }

        return 0;
    }
}
