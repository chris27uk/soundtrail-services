using Soundtrail.Domain;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

internal sealed class CommandBusFake : ICommandBus
{
    private readonly List<ICommand> sentCommands = [];

    public IReadOnlyList<ICommand> SentCommands => sentCommands;

    public Task SendAsync(ICommand command, CancellationToken cancellationToken = default)
    {
        sentCommands.Add(command);
        return Task.CompletedTask;
    }
}
