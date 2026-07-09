using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Operations;
using Soundtrail.Tools.Operations.Infrastructure.CommandLine;

namespace Soundtrail.Tools.Operations.Features.ImportKworbChart;

public sealed class ImportKworbChartCommandLineHandler(
    IHandler<ImportKworbChartCommand> handler) : ICommandLineOptionsHandler
{
    public Type OptionsType => typeof(ImportKworbChartCommandLineOptions);

    public async Task<int> HandleAsync(object options, CancellationToken cancellationToken)
    {
        await handler.Handle(new ImportKworbChartCommand(), cancellationToken);
        return 0;
    }
}
