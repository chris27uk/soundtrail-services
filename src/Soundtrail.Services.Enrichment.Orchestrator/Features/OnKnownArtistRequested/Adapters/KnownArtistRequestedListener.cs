using Raven.Client.Documents.Session;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Translators.Api;
using Wolverine.Attributes;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownArtistRequested.Adapters;

public sealed class KnownArtistRequestedListener(KnownArtistRequestedHandler handler)
{
    [WolverineHandler]
    [Transactional]
    public Task Handle(
        KnownArtistRequestedDto dto,
        IAsyncDocumentSession _,
        CancellationToken cancellationToken = default) =>
        handler.Handle(ApiCommandMessageTranslator.ToDomainObject(dto), cancellationToken);
}
