using Soundtrail.Domain.Abstractions;

namespace Soundtrail.Services.Enrichment.Orchestrator.Infrastructure.Messaging;

public sealed class WolverineCommandBus(Wolverine.IMessageBus messageBus) : Soundtrail.Domain.ICommandBus
{
    public Task SendAsync(ICommand command, CancellationToken cancellationToken = default)
    {
        return messageBus.SendAsync(command.ToMessage()).AsTask();
    }
}
