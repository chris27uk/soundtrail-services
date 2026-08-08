using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Operations;
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

        public ImportKworbChartCommand? Request { get; private set; }

        public Task Handle(IncomingMessage<ImportKworbChartCommand> context, CancellationToken cancellationToken = default)
        {
            Calls++;
            Request = context.Message;
            return Task.CompletedTask;
        }
    }
}
