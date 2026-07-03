using Raven.Client.Documents.Session;
using Soundtrail.Adapters.Registry;
using Soundtrail.Contracts.IntegrationMessaging.Responses;
using Soundtrail.Domain.Enrichment.Responses;
using Wolverine.Attributes;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnCatalogItemLookupAttempted.Adapters;

public sealed class CatalogItemLookupAttemptedListener(CatalogItemLookupAttemptedHandler handler)
{
    [WolverineHandler]
    [Transactional]
    public Task Handle(
        CatalogItemLookupAttemptedDto dto,
        IAsyncDocumentSession _,
        CancellationToken cancellationToken = default) =>
        handler.Handle(TypeTranslationRegistry.Default.ToDomainObject<CatalogItemLookupAttempted>(dto), cancellationToken);
}
