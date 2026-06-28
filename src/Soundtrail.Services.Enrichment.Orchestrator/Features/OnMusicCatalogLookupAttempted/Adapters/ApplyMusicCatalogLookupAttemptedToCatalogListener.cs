using Raven.Client.Documents.Session;
using Soundtrail.Adapters.Registry;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Domain.Enrichment.Commands;
using Wolverine.Attributes;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnMusicCatalogLookupAttempted.Adapters;

public sealed class ApplyMusicCatalogLookupAttemptedToCatalogListener(ApplyMusicCatalogLookupAttemptedToCatalogHandler handler)
{
    [WolverineHandler]
    [Transactional]
    public Task Handle(
        ApplyMusicCatalogLookupAttemptedToCatalogCommandDto dto,
        IAsyncDocumentSession _,
        CancellationToken cancellationToken = default) =>
        handler.Handle(TypeTranslationRegistry.Default.ToDomainObject<ApplyMusicCatalogLookupAttemptedToCatalogCommand>(dto), cancellationToken);
}
