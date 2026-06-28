using Raven.Client.Documents.Session;
using Soundtrail.Adapters.Registry;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Domain.Enrichment.Commands;
using Wolverine.Attributes;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnMusicCatalogLookupAttempted.Adapters;

public sealed class ApplyMusicCatalogLookupAttemptedToDiscoveryListener(ApplyMusicCatalogLookupAttemptedToDiscoveryHandler handler)
{
    [WolverineHandler]
    [Transactional]
    public Task Handle(
        ApplyMusicCatalogLookupAttemptedToDiscoveryCommandDto dto,
        IAsyncDocumentSession _,
        CancellationToken cancellationToken = default) =>
        handler.Handle(TypeTranslationRegistry.Default.ToDomainObject<ApplyMusicCatalogLookupAttemptedToDiscoveryCommand>(dto), cancellationToken);
}
