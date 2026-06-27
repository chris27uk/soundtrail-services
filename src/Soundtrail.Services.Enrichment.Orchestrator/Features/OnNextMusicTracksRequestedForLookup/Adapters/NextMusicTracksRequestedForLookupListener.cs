using Raven.Client.Documents.Session;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Domain.Enrichment.Commands;
using Wolverine.Attributes;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnNextMusicTracksRequestedForLookup.Adapters;

public sealed class NextMusicTracksRequestedForLookupListener(NextMusicTracksRequestedForLookupHandler handler)
{
    [WolverineHandler]
    [Transactional]
    public async Task Handle(RunDiscoveryBacklogSchedulingCommandDto message, IAsyncDocumentSession session, CancellationToken cancellationToken = default)
    {
        await handler.Handle(
            new RunDiscoveryBacklogSchedulingCommand(
                CommandId.For($"RunDiscoveryBacklogScheduling:{message.Now.ToUnixTimeMilliseconds()}"),
                message.Now,
                CorrelationId.New(),
                message.Take),
            cancellationToken);
    }
}
