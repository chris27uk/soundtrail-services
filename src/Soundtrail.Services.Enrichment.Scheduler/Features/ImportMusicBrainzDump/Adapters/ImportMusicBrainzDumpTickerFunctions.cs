using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog.MusicBrainzDumpImport;
using Soundtrail.Domain.Operations;
using TickerQ.Utilities.Base;
using TickerQ.Utilities.Enums;

namespace Soundtrail.Services.Enrichment.Scheduler.Features.ImportMusicBrainzDump.Adapters;

public sealed class ImportMusicBrainzDumpTickerRequest
{
    public string? DumpVersion { get; init; }
}

public sealed class ImportMusicBrainzDumpTickerFunctions(
    IScheduledMessageHandler<ImportMusicBrainzDumpCommand> handler)
{
    public const string FunctionName = "ImportMusicBrainzDump";
    public const string ManualFunctionName = "ImportMusicBrainzDumpSnapshot";
    public const string DefaultCronExpression = "0 0 1 * *";

    [TickerFunction(FunctionName, DefaultCronExpression, TickerTaskPriority.Normal, 1)]
    public Task ImportMusicBrainzDump(TickerFunctionContext _, CancellationToken cancellationToken) =>
        handler.HandleAsync(ImportMusicBrainzDumpCommand.ForScheduled(DateTimeOffset.UtcNow), cancellationToken);

    [TickerFunction(ManualFunctionName, TickerTaskPriority.Normal)]
    public Task ImportMusicBrainzDumpSnapshot(
        TickerFunctionContext<ImportMusicBrainzDumpTickerRequest> context,
        CancellationToken cancellationToken)
    {
        var dumpVersion = context.Request?.DumpVersion;
        if (string.IsNullOrWhiteSpace(dumpVersion))
        {
            throw new InvalidOperationException(
                "Manual ImportMusicBrainzDumpSnapshot requires request.DumpVersion (concrete snapshot id).");
        }

        var snapshotId = MusicBrainzDumpSnapshotId.Parse(dumpVersion);
        return handler.HandleAsync(
            ImportMusicBrainzDumpCommand.ForManual(DateTimeOffset.UtcNow, snapshotId),
            cancellationToken);
    }
}
