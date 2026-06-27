using Raven.Client.Documents.Session;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Internal.Projector.Features.OnMusicTrackSearchStarted.Adapters.Documents;
using Soundtrail.Services.Internal.Projector.Features.OnMusicTrackSearchStarted.Ports;

namespace Soundtrail.Services.Internal.Projector.Features.OnMusicTrackSearchStarted.Adapters;

public sealed class RavenSaveCatalogSearchStartedTracking(IAsyncDocumentSession session) : ISaveCatalogSearchStartedTrackingPort
{
    public async Task SaveAsync(
        CatalogSearchCriteria criteria,
        MusicCatalogId musicCatalogId,
        DateTimeOffset updatedAt,
        CancellationToken cancellationToken)
    {
        var documentId = CatalogSearchTrackingRecordDto.GetDocumentId(criteria.Value);
        var document = await session.LoadAsync<CatalogSearchTrackingRecordDto>(documentId, cancellationToken)
            ?? new CatalogSearchTrackingRecordDto
            {
                Id = documentId
            };

        document.Criteria = criteria.Value;
        document.MusicCatalogId = musicCatalogId.Value;
        document.UpdatedAt = updatedAt;
        await session.StoreAsync(document, cancellationToken);
    }
}
