using Raven.Client.Documents.Session;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Wolverine.Attributes;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnNextMusicTracksRequestedForLookup.Adapters;

public sealed class NextMusicTracksRequestedForLookupListener(NextMusicTracksRequestedForLookupHandler handler)
{
    [WolverineHandler]
    [Transactional]
    public async Task Handle(RunDiscoveryBacklogSchedulingCommandDto message, IAsyncDocumentSession session, CancellationToken cancellationToken = default)
    {
        await handler.RunOnceAsync(message.Now, message.Take, cancellationToken);
    }
}
