using Raven.Client.Documents.Session;
using Soundtrail.Contracts;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchStatusChanged.Ports;
using Soundtrail.Domain.Discovery;
using Soundtrail.Translators.Discovery;

namespace Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchStatusChanged.Adapters;

public sealed class RavenSaveDiscoveryLifecycleProjection(
    IAsyncDocumentSession session,
    RavenDiscoveryLifecycleProjectionMapper mapper) : ISaveDiscoveryLifecycleProjectionPort
{
    public async Task SaveAsync(
        DiscoveryLifecycleProjection projection,
        CancellationToken cancellationToken)
    {
        var persistentId = MusicSearchTermPersistentIdTranslator.ToPersistentId(projection.SearchCriteria);
        var statusDocumentId = CatalogSearchStatusRecordDto.GetDocumentId(persistentId);
        var statusDocument = await session.LoadAsync<CatalogSearchStatusRecordDto>(statusDocumentId, cancellationToken)
            ?? new CatalogSearchStatusRecordDto
            {
                Id = statusDocumentId
            };

        mapper.MapOntoStatusDocument(statusDocument, projection);
        await session.StoreAsync(statusDocument, cancellationToken);

        var checkpointDocumentId = DiscoveryLifecycleProjectionCheckpointDocument.GetDocumentId(persistentId);
        var checkpointDocument = await session.LoadAsync<DiscoveryLifecycleProjectionCheckpointDocument>(checkpointDocumentId, cancellationToken)
            ?? new DiscoveryLifecycleProjectionCheckpointDocument
            {
                Id = checkpointDocumentId
            };

        mapper.MapOntoCheckpointDocument(checkpointDocument, projection);
        await session.StoreAsync(checkpointDocument, cancellationToken);

        await session.SaveChangesAsync(cancellationToken);
    }
}
