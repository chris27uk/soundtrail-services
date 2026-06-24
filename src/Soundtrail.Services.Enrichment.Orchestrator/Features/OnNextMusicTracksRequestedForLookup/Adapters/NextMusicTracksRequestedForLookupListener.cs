using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Raven.Client.Documents.Session;
using Wolverine.Attributes;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnNextMusicTracksRequestedForLookup.Adapters;

public sealed class NextMusicTracksRequestedForLookupListener(
    NextMusicTracksRequestedForLookupHandler scheduler)
{
    [WolverineHandler]
    [Transactional]
    public async Task Handle(RunDiscoveryBacklogSchedulingCommandDto message, IAsyncDocumentSession session, CancellationToken cancellationToken = default)
    {
        await scheduler.RunOnceAsync(message.Now, message.Take, cancellationToken);
    }
}
