using Raven.Client.Documents.Session;
using Soundtrail.Contracts;
using Soundtrail.Domain.Discovery;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.ProjectDiscoveryLifecycle.Adapters;

public sealed class RavenSaveDiscoveryLifecycleProjection(
    IAsyncDocumentSession session,
    RavenDiscoveryLifecycleProjectionMapper mapper) : ISaveDiscoveryLifecycleProjectionPort
{
    public async Task SaveAsync(
        DiscoveryLifecycleProjection projection,
        CancellationToken cancellationToken)
    {
        var statusDocumentId = CatalogSearchStatusRecordDto.GetDocumentId(projection.Criteria.Value);
        var statusDocument = await session.LoadAsync<CatalogSearchStatusRecordDto>(statusDocumentId, cancellationToken)
            ?? new CatalogSearchStatusRecordDto
            {
                Id = statusDocumentId
            };

        mapper.MapOntoStatusDocument(statusDocument, projection);
        await session.StoreAsync(statusDocument, cancellationToken);

        var checkpointDocumentId = DiscoveryLifecycleProjectionCheckpointDocument.GetDocumentId(projection.Criteria.Value);
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
