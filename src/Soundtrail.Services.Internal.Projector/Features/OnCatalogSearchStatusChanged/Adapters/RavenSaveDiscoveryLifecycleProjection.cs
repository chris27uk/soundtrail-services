using Raven.Client.Documents.Session;
using Soundtrail.Contracts;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchStatusChanged.Ports;
using Soundtrail.Adapters.Discovery;
using Soundtrail.Adapters.Registry;

namespace Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchStatusChanged.Adapters;

public sealed class RavenSaveDiscoveryLifecycleProjection(
    IAsyncDocumentSession session,
    ITypeTranslator translator) : ISaveDiscoveryLifecycleProjectionPort
{
    public async Task SaveAsync(
        DiscoveryLifecycleProjection projection,
        CancellationToken cancellationToken)
    {
        var persistentId = DiscoveryQueryKey.StableValueFor(projection.SearchCriteria);
        var statusDocumentId = CatalogSearchStatusRecordDto.GetDocumentId(persistentId);
        var statusDocument = await session.LoadAsync<CatalogSearchStatusRecordDto>(statusDocumentId, cancellationToken)
            ?? new CatalogSearchStatusRecordDto
            {
                Id = statusDocumentId
            };

        translator.MapOnto(projection, statusDocument);
        await session.StoreAsync(statusDocument, cancellationToken);

        var checkpointDocumentId = DiscoveryLifecycleProjectionCheckpointDocument.GetDocumentId(persistentId);
        var checkpointDocument = await session.LoadAsync<DiscoveryLifecycleProjectionCheckpointDocument>(checkpointDocumentId, cancellationToken)
            ?? new DiscoveryLifecycleProjectionCheckpointDocument
            {
                Id = checkpointDocumentId
            };

        translator.MapOnto(projection, checkpointDocument);
        await session.StoreAsync(checkpointDocument, cancellationToken);

        await session.SaveChangesAsync(cancellationToken);
    }
}
