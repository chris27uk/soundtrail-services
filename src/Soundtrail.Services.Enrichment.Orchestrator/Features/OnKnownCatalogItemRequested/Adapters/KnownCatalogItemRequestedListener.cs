using Raven.Client.Documents.Session;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Wolverine.Attributes;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownCatalogItemRequested.Adapters;

public sealed class KnownCatalogItemRequestedListener(KnownCatalogItemRequestedHandler handler)
{
    [WolverineHandler]
    [Transactional]
    public Task Handle(
        KnownCatalogItemRequestedDto dto,
        IAsyncDocumentSession _,
        CancellationToken cancellationToken = default) =>
        handler.Handle(KnownCatalogItemRequestedDtoMapper.ToDomainObject(dto), cancellationToken);
}
