using Microsoft.Extensions.Logging;
using Soundtrail.Tools.MusicBrainzImport.Features.ReplayCatalogProjection;

namespace Soundtrail.Tools.MusicBrainzImport.Features.ImportMusicBrainzDump.Adapters;

public sealed class MusicBrainzDumpCommandLineRunner(
    ImportMusicBrainzDumpHandler handler,
    ReplayCatalogProjectionHandler replayCatalogProjectionHandler,
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
                    "Imported MusicBrainz dump records. Processed: {Processed}, Imported: {Imported}, Projected: {Projected}, Skipped: {Skipped}",
                    importResult.ProcessedRecordCount,
                    importResult.ImportedRecordCount,
                    importResult.ProjectedRecordCount,
                    importResult.SkippedRecordCount);

                Console.WriteLine(
                    $"Processed={importResult.ProcessedRecordCount} Imported={importResult.ImportedRecordCount} Projected={importResult.ProjectedRecordCount} Skipped={importResult.SkippedRecordCount}");
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
            default:
                throw new InvalidOperationException($"Unsupported tool command type '{command.GetType().Name}'.");
        }

        return 0;
    }
}
