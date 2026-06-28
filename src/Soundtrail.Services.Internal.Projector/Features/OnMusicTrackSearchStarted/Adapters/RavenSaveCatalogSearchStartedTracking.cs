using Raven.Client.Documents.Session;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchCandidateRecorded.Adapters.Documents;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchCandidateRecorded.Ports;
using Soundtrail.Adapters.Discovery;

namespace Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchCandidateRecorded.Adapters;

public sealed class RavenSaveCatalogSearchCandidateTracking(IAsyncDocumentSession session) : ISaveCatalogSearchCandidateTrackingPort
{
    public async Task SaveAsync(
        MusicSearchCriteria searchCriteria,
        MusicCatalogId musicCatalogId,
        DateTimeOffset updatedAt,
        CancellationToken cancellationToken)
    {
        var persistentId = DiscoveryQueryKey.StableValueFor(searchCriteria);
        var documentId = CatalogSearchTrackingRecordDto.GetDocumentId(persistentId);
        var document = await session.LoadAsync<CatalogSearchTrackingRecordDto>(documentId, cancellationToken)
            ?? new CatalogSearchTrackingRecordDto
            {
                Id = documentId
            };

        document.Criteria = persistentId;
        document.MusicCatalogId = musicCatalogId.Value;
        document.UpdatedAt = updatedAt;
        await session.StoreAsync(document, cancellationToken);
    }
}
