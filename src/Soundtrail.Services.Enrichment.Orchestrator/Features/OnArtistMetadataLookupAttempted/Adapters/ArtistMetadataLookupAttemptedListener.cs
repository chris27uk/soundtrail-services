using Raven.Client.Documents.Session;
using Soundtrail.Adapters.Registry;
using Soundtrail.Contracts.IntegrationMessaging.Responses;
using Soundtrail.Domain.Enrichment.Responses;
using Wolverine.Attributes;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnArtistMetadataLookupAttempted.Adapters;

public sealed class ArtistMetadataLookupAttemptedListener(ArtistMetadataLookupAttemptedHandler handler)
{
    [WolverineHandler]
    [Transactional]
    public Task Handle(
        ArtistMetadataLookupAttemptedDto dto,
        IAsyncDocumentSession _,
        CancellationToken cancellationToken = default) =>
        handler.Handle(TypeTranslationRegistry.Default.ToDomainObject<ArtistMetadataLookupAttempted>(dto), cancellationToken);
}
