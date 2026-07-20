using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Operations;
using TickerQ.Utilities.Base;
using TickerQ.Utilities.Enums;

namespace Soundtrail.Services.Enrichment.Scheduler.Features.ImportKworbChart.Adapters;

public sealed class ImportKworbChartTickerFunctions(IHandler<ImportKworbChartCommand> handler)
{
    public const string FunctionName = "ImportKworbChart";
    public const string DefaultCronExpression = "0 * * * *";

    [TickerFunction(FunctionName, DefaultCronExpression, TickerTaskPriority.Normal, 1)]
    public async Task ImportKworbChart(TickerFunctionContext _, CancellationToken cancellationToken)
    {
        await handler.Handle(new ImportKworbChartCommand(DateTimeOffset.UtcNow), cancellationToken);
    }
}
