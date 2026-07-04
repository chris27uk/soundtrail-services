using Raven.Client.Documents.Session;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Adapters.Registry;
using Wolverine.Attributes;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnCatalogSearchRequested.Adapters;

public sealed class SearchCatalogRequestedListener(SearchCatalogRequestedHandler handler)
{
    [WolverineHandler]
    [Transactional]
    public async Task Handle(
        CatalogSearchAttemptDto requestDto,
        IAsyncDocumentSession _,
        CancellationToken cancellationToken = default)
    {
        await handler.Handle(
            TypeTranslationRegistry.Default.ToDomainObject<SearchCatalogRequested>(requestDto),
            cancellationToken);
    }
}
