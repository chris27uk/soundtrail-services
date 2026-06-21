using Microsoft.Extensions.Logging;
using Soundtrail.Domain.Commands;

namespace Soundtrail.Tools.MusicBrainzImport.Features.ImportMusicBrainzDump.Adapters;

public sealed class MusicBrainzDumpCommandLineRunner(
    ImportMusicBrainzDumpHandler handler,
    ILogger<MusicBrainzDumpCommandLineRunner> logger)
{
    public async Task<int> RunAsync(
        ImportMusicBrainzDumpCommand command,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(command, cancellationToken);
        logger.LogInformation(
            "Imported MusicBrainz dump records. Processed: {Processed}, Imported: {Imported}, Projected: {Projected}, Skipped: {Skipped}",
            result.ProcessedRecordCount,
            result.ImportedRecordCount,
            result.ProjectedRecordCount,
            result.SkippedRecordCount);

        Console.WriteLine(
            $"Processed={result.ProcessedRecordCount} Imported={result.ImportedRecordCount} Projected={result.ProjectedRecordCount} Skipped={result.SkippedRecordCount}");
        return 0;
    }
}
