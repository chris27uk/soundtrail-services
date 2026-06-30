using Raven.Client.Documents.Session;
using Soundtrail.Adapters.Registry;
using Soundtrail.Contracts.IntegrationMessaging.Responses;
using Soundtrail.Domain.Enrichment.Responses;
using Wolverine.Attributes;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnAlbumMetadataLookupAttempted.Adapters;

public sealed class AlbumMetadataLookupAttemptedListener(AlbumMetadataLookupAttemptedHandler handler)
{
    [WolverineHandler]
    [Transactional]
    public Task Handle(
        AlbumMetadataLookupAttemptedDto dto,
        IAsyncDocumentSession _,
        CancellationToken cancellationToken = default) =>
        handler.Handle(TypeTranslationRegistry.Default.ToDomainObject<AlbumMetadataLookupAttempted>(dto), cancellationToken);
}
