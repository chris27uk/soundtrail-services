using Soundtrail.Domain.Abstractions;
using Soundtrail.Services.Enrichment.Scheduler.Features.SendDiscoveryBacklogMessage.Adapters;
using Wolverine;
using ICommandBus = Soundtrail.Domain.Abstractions.ICommandBus;

namespace Soundtrail.Services.Enrichment.Scheduler.Infrastructure.Messaging;

public sealed class WolverineCommandBus(IMessageBus messageBus) : ICommandBus
{
    public Task SendAsync(ICommand command, CancellationToken cancellationToken = default)
    {
        return messageBus.SendAsync(command.ToMessage()).AsTask();
    }
}
