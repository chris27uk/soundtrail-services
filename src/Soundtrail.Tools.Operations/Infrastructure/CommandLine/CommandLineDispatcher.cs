using CommandLine;

namespace Soundtrail.Tools.Operations.Infrastructure.CommandLine;

public sealed class CommandLineDispatcher(IEnumerable<ICommandLineOptionsHandler> handlers)
{
    private readonly IReadOnlyList<ICommandLineOptionsHandler> handlers = handlers.ToArray();

    public Task<int> DispatchAsync(string[] args, CancellationToken cancellationToken)
    {
        if (args.Length == 0 || handlers.Count == 0)
        {
            return Task.FromResult(1);
        }

        var result = Parser.Default.ParseArguments(args, handlers.Select(handler => handler.OptionsType).ToArray());

        return result.MapResult(
            parsedOptions => HandleParsedOptionsAsync(parsedOptions, cancellationToken),
            _ => Task.FromResult(1));
    }

    private Task<int> HandleParsedOptionsAsync(object parsedOptions, CancellationToken cancellationToken)
    {
        var handler = handlers.Single(candidate => candidate.OptionsType == parsedOptions.GetType());
        return handler.HandleAsync(parsedOptions, cancellationToken);
    }
}
