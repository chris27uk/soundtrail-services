namespace Soundtrail.Tools.Operations.Infrastructure.CommandLine;

public interface ICommandLineOptionsHandler
{
    Type OptionsType { get; }

    Task<int> HandleAsync(object options, CancellationToken cancellationToken);
}
