using Raven.Client.Documents.Session;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Internal.Projector.Features.OnMusicTrackSearchStarted.Adapters.Documents;
using Soundtrail.Services.Internal.Projector.Features.OnMusicTrackSearchStarted.Ports;
using Soundtrail.Translators.Discovery;

namespace Soundtrail.Services.Internal.Projector.Features.OnMusicTrackSearchStarted.Adapters;

public sealed class RavenSaveCatalogSearchStartedTracking(IAsyncDocumentSession session) : ISaveCatalogSearchStartedTrackingPort
{
    public async Task SaveAsync(
        MusicSearchCriteria searchCriteria,
        MusicCatalogId musicCatalogId,
        DateTimeOffset updatedAt,
        CancellationToken cancellationToken)
    {
        var persistentId = MusicSearchTermPersistentIdTranslator.ToPersistentId(searchCriteria);
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
