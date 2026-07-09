using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Operations;
using Soundtrail.Tools.Operations.Features.ImportKworbChart;
using Soundtrail.Tools.Operations.Infrastructure.CommandLine;

namespace Soundtrail.Services.Tests.Integration.CommandLine.ImportKworbChart;

internal sealed class ImportKworbChartCommandLineTestEnvironment
{
    private ImportKworbChartCommandLineTestEnvironment(
        CommandLineDispatcher subject,
        ImportKworbChartHandlerFake handler)
    {
        Subject = subject;
        Handler = handler;
    }

    public CommandLineDispatcher Subject { get; }

    public ImportKworbChartHandlerFake Handler { get; }

    public static ImportKworbChartCommandLineTestEnvironment Create()
    {
        var handler = new ImportKworbChartHandlerFake();
        var dispatcher = new CommandLineDispatcher([new ImportKworbChartCommandLineHandler(handler)]);
        return new ImportKworbChartCommandLineTestEnvironment(dispatcher, handler);
    }

    internal sealed class ImportKworbChartHandlerFake : IHandler<ImportKworbChartCommand>
    {
        public int Calls { get; private set; }

        public Task Handle(ImportKworbChartCommand request, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.CompletedTask;
        }
    }
}
