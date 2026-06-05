using Raven.Client.Documents.Session;
using Soundtrail.Services.Enrichment.Features.BacklogScheduling;
using Soundtrail.Services.Enrichment.Scheduler.Infrastructure.Messaging;
using Wolverine.Attributes;

namespace Soundtrail.Services.Enrichment.Scheduler.Infrastructure.Scheduling;

public sealed class DiscoveryBacklogSchedulingListener(DiscoveryBacklogScheduler scheduler)
{
    [WolverineHandler]
    [Transactional]
    public async Task<object[]> Handle(
        RunDiscoveryBacklogScheduling message,
        IAsyncDocumentSession session,
        CancellationToken cancellationToken = default)
    {
        var commands = await scheduler.RunOnceAsync(message.Now, message.Take, cancellationToken);
        return commands.Select(command => command.ToResolveCanonicalMetadataTransportMessage()).ToArray();
    }
}
