using Raven.Client.Documents.Session;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Translators.Api;
using Wolverine.Attributes;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownAlbumRequested.Adapters;

public sealed class KnownAlbumRequestedListener(KnownAlbumRequestedHandler handler)
{
    [WolverineHandler]
    [Transactional]
    public Task Handle(
        KnownAlbumRequestedDto dto,
        IAsyncDocumentSession _,
        CancellationToken cancellationToken = default) =>
        handler.Handle(ApiCommandMessageTranslator.ToDomainObject(dto), cancellationToken);
}
