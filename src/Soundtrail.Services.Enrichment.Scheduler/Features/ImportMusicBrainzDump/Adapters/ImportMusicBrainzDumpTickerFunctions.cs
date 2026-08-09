using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Operations;
using TickerQ.Utilities.Base;
using TickerQ.Utilities.Enums;

namespace Soundtrail.Services.Enrichment.Scheduler.Features.ImportMusicBrainzDump.Adapters;

public sealed class ImportMusicBrainzDumpTickerFunctions(
    IScheduledMessageHandler<ImportMusicBrainzDumpCommand> handler)
{
    public const string FunctionName = "ImportMusicBrainzDump";
    public const string DefaultCronExpression = "0 0 1 * *";

    [TickerFunction(FunctionName, DefaultCronExpression, TickerTaskPriority.Normal, 1)]
    public Task ImportMusicBrainzDump(TickerFunctionContext _, CancellationToken cancellationToken) =>
        handler.HandleAsync(new ImportMusicBrainzDumpCommand(DateTimeOffset.UtcNow), cancellationToken);
}
