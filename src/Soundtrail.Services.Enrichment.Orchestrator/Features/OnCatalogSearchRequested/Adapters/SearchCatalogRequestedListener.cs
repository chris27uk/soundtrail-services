using Raven.Client.Documents.Session;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Translators.Api;
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
            ApiCommandMessageTranslator.ToDomainObject(requestDto),
            cancellationToken);
    }
}
