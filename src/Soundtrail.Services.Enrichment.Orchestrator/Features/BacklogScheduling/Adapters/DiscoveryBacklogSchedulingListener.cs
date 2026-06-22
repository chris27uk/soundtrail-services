using Raven.Client.Documents.Session;
using Soundtrail.Services.Enrichment.Orchestrator.Features.BacklogScheduling;
using Soundtrail.Services.Enrichment.Orchestrator.Infrastructure.Messaging;
using Wolverine;
using Soundtrail.Services.Enrichment.Orchestrator.Infrastructure.Scheduling;
using Wolverine.Attributes;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.BacklogScheduling.Adapters;

public sealed class DiscoveryBacklogSchedulingListener(
    DiscoveryBacklogScheduler scheduler,
    IMessageBus messageBus)
{
    [WolverineHandler]
    [Transactional]
    public async Task Handle(
        RunDiscoveryBacklogScheduling message,
        IAsyncDocumentSession session,
        CancellationToken cancellationToken = default)
    {
        var commands = await scheduler.RunOnceAsync(message.Now, message.Take, cancellationToken);

        foreach (var outgoingMessage in commands.Select(command => command.ToMessage()))
        {
            await messageBus.SendAsync(outgoingMessage);
        }
    }
}
