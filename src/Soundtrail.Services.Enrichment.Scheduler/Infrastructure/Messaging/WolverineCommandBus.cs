using Soundtrail.Domain.Abstractions;
using Soundtrail.Services.Enrichment.Scheduler.Features.SendDiscoveryBacklogMessage.Adapters;

namespace Soundtrail.Services.Enrichment.Scheduler.Infrastructure.Messaging;

public sealed class WolverineCommandBus(Wolverine.IMessageBus messageBus) : Soundtrail.Domain.ICommandBus
{
    public Task SendAsync(ICommand command, CancellationToken cancellationToken = default)
    {
        return messageBus.SendAsync(command.ToMessage()).AsTask();
    }
}
