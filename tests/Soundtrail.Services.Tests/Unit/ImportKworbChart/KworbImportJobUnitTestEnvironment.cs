using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Operations;
using Soundtrail.Services.Enrichment.Scheduler.Features.ImportKworbChart;
using Soundtrail.Services.Enrichment.Scheduler.Features.ImportKworbChart.Adapters;

namespace Soundtrail.Services.Tests.Unit.ImportKworbChart;

internal sealed class KworbImportJobUnitTestEnvironment
{
    private KworbImportJobUnitTestEnvironment(ImportKworbChartHandlerFake handler)
    {
        Handler = handler;
    }

    public ImportKworbChartHandlerFake Handler { get; }

    public static KworbImportJobUnitTestEnvironment Create() => new(new ImportKworbChartHandlerFake());

    public ImportKworbChartTickerFunctions CreateSubjectUnderTest() => new(Handler);

    public sealed class ImportKworbChartHandlerFake : IHandler<ImportKworbChartCommand>
    {
        public int Calls { get; private set; }

        public Task Handle(ImportKworbChartCommand request, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.CompletedTask;
        }
    }
}
