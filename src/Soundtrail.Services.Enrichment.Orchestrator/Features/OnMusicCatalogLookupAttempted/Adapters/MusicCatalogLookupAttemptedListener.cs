using Raven.Client.Documents.Session;
using Soundtrail.Adapters.Registry;
using Soundtrail.Contracts.IntegrationMessaging.Responses;
using Soundtrail.Domain.Enrichment.Responses;
using Wolverine.Attributes;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnMusicCatalogLookupAttempted.Adapters;

public sealed class MusicCatalogLookupAttemptedListener(MusicCatalogLookupAttemptedHandler handler)
{
    [WolverineHandler]
    [Transactional]
    public Task Handle(
        MusicCatalogLookupAttemptedDto dto,
        IAsyncDocumentSession _,
        CancellationToken cancellationToken = default) =>
        handler.Handle(TypeTranslationRegistry.Default.ToDomainObject<MusicCatalogLookupAttempted>(dto), cancellationToken);
}
