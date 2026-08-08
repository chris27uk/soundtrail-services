using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Enrichment.Scheduler.Features.ImportKworbChart;
using Soundtrail.Services.Enrichment.Scheduler.Features.ImportKworbChart.Adapters;
using Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.Fakes;

namespace Soundtrail.Services.Tests.Unit.ImportKworbChart;

internal sealed class KworbImportJobUnitTestEnvironment
{
    private KworbImportJobUnitTestEnvironment()
    {
        CommandBus = new CommandBusFake();
        Handler = new ImportKworbChartHandler(CommandBus);
    }

    public CommandBusFake CommandBus { get; }

    public ImportKworbChartHandler Handler { get; }

    public static KworbImportJobUnitTestEnvironment Create() => new();

    public ImportKworbChartTickerFunctions CreateSubjectUnderTest() => new(Handler);
}
