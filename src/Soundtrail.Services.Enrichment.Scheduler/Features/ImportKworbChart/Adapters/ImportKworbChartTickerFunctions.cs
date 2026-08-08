using Soundtrail.Adapters.Messaging;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Operations;
using TickerQ.Utilities.Base;
using TickerQ.Utilities.Enums;

namespace Soundtrail.Services.Enrichment.Scheduler.Features.ImportKworbChart.Adapters;

public sealed class ImportKworbChartTickerFunctions(ISchedulerHandler<ImportKworbChartCommand> handler)
{
    public const string FunctionName = "ImportKworbChart";
    public const string DefaultCronExpression = "0 * * * *";

    [TickerFunction(FunctionName, DefaultCronExpression, TickerTaskPriority.Normal, 1)]
    public Task ImportKworbChart(TickerFunctionContext _, CancellationToken cancellationToken) =>
        ScheduledMessageTelemetry.ExecuteAsync(
            new ImportKworbChartCommand(DateTimeOffset.UtcNow),
            FunctionName,
            handler.HandleAsync,
            cancellationToken);
}
