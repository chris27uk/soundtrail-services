namespace Soundtrail.Services.Enrichment.Orchestrator.Infrastructure.Messaging;

public sealed class WolverineCommandBus(Wolverine.IMessageBus messageBus) : Soundtrail.Domain.ICommandBus
{
    public Task SendAsync(Soundtrail.Domain.ICommand command, CancellationToken cancellationToken = default)
    {
        return messageBus.SendAsync(command.ToMessage()).AsTask();
    }
}
