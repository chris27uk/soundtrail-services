using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.ApplyLookupExecutionReport.Support;
using Raven.Client.Documents.Session;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.BacklogScheduling;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Infrastructure.Messaging;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Infrastructure.Scheduling;
using Wolverine.Attributes;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.BacklogScheduling.Adapters;

public sealed class DiscoveryBacklogSchedulingListener(
    DiscoveryBacklogScheduler scheduler,
    CatalogSearchDiscoveryByMusicCatalogIdTransitionApplier transitionApplier)
{
    [WolverineHandler]
    [Transactional]
    public async Task<object[]> Handle(
        RunDiscoveryBacklogScheduling message,
        IAsyncDocumentSession session,
        CancellationToken cancellationToken = default)
    {
        var commands = await scheduler.RunOnceAsync(message.Now, message.Take, cancellationToken);
        foreach (var command in commands)
        {
            await transitionApplier.ApplyAsync(
                command.MusicCatalogId,
                discovery => discovery.Start(command.Priority, "Lookup started", message.Now),
                cancellationToken);
        }

        return commands.Select(command => command.ToMessage()).ToArray();
    }
}
