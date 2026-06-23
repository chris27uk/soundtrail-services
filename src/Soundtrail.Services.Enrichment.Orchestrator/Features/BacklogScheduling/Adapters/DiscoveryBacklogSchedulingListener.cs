using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Raven.Client.Documents.Session;
using Wolverine.Attributes;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.BacklogScheduling.Adapters;

public sealed class DiscoveryBacklogSchedulingListener(
    DiscoveryBacklogScheduler scheduler)
{
    [WolverineHandler]
    [Transactional]
    public async Task Handle(RunDiscoveryBacklogSchedulingCommandDto message, IAsyncDocumentSession session, CancellationToken cancellationToken = default)
    {
        await scheduler.RunOnceAsync(message.Now, message.Take, cancellationToken);
    }
}
